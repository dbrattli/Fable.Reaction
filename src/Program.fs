namespace Fable.Reaction

open System

open Elmish
open Reaction
open Reaction.AsyncObservable
open Reaction.Streams

[<RequireQualifiedAccess>]
module Program =
    /// Attach a Reaction query to the message (Msg) stream of an Elmish program.
    /// The supplied query function is called once by the Elmish runtime.
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

    /// Experimental!
    /// Attach a named Reaction query to the message (Msg) stream of an Elmish program.
    /// The supplied query function will be called every time the model is updated.
    /// The returned query must be named using the `named` operator. If the query
    /// function retures a query with a new name, then the previous query will
    /// be disposed and the new query will be subscribed. This makes it possible to
    /// dynamically change the query at runtime based on the current state (Model).
    let withNamedQuery (query: 'model -> IAsyncObservable<'msg> -> INamedAsyncObservable<'msg>) (program: Elmish.Program<_,_,_,_>) =
        let mutable dispatch' : Dispatch<'msg> = ignore
        let mutable currentMsgs = empty () |> named "noop"
        let mutable subscription = AsyncDisposable.Empty
        let mutable running = false
        let mb, stream = mbStream<'msg> ()
        let post = OnNext >> mb.Post

        let view model dispatch =
            let msgs = query model stream

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

                do! subscription.DisposeAsync ()
                currentMsgs <- msgs
                let! disposable = msgs.SubscribeAsync msgObserver
                subscription <- disposable
            }
            if not running || msgs.Name <> currentMsgs.Name then
                running <- true
                Async.StartImmediate main

            dispatch' <- dispatch
            program.view model post

        { program with view = view }
