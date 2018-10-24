namespace Fable.Reaction

open System

open Elmish
open Reaction
open Reaction.AsyncObservable
open Reaction.Streams

[<RequireQualifiedAccess>]
module Program =
    /// Attach a Reaction query to the message (Msg) stream of an Elmish program.
    let withQuery (query: IAsyncObservable<'msg> -> IAsyncObservable<'msg>) (program: Elmish.Program<_,_,_,_>) =
        let mutable dispatch' : Dispatch<'msg> = ignore
        let mb, stream = mbStream<'msg> ()
        let mutable running = false
        let post = OnNext >> mb.Post

        let main = async {
            let msgObserver =
                { new IAsyncObserver<'msg> with
                    member this.OnNextAsync x = async {
                        dispatch' x
                    }
                    member this.OnErrorAsync err = async {
                        program.onError ("Reaction query error", err)
                    }
                    member this.OnCompletedAsync () = async {
                        program.onError ("Reaction query completed", Exception ())
                    }
                }

            let msgs = query stream
            do! msgs.RunAsync msgObserver
        }

        let view model dispatch =
            if not running then
                running <- true
                Async.StartImmediate main

            dispatch' <- dispatch
            program.view model post

        { program with view = view }

