namespace Fable.Reaction

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

        let view model dispatch =
            dispatch' <- dispatch
            program.view model (OnNext >> mb.Post)

        let main = async {
            let msgObserver =
                { new IAsyncObserver<'msg> with
                    member this.OnNextAsync x = async {
                        dispatch' x
                    }
                    member this.OnErrorAsync err = async {
                        program.onError ("Query error", err)
                    }
                    member this.OnCompletedAsync () = async {
                        ()
                    }
                }

            let msgs = query stream
            do! msgs.RunAsync msgObserver
        }
        Async.StartImmediate main

        { program with view = view }

