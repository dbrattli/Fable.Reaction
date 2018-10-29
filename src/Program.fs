namespace Fable.Reaction

open System

open Elmish
open Reaction
open Reaction.AsyncObservable
open Reaction.Streams
open Reaction

[<RequireQualifiedAccess>]
module Program =
    /// Attach a Reaction query to the message (Msg) stream of an Elmish program.
    /// The supplied query function is called once by the Elmish runtime.
    let withQuery (query: IAsyncObservable<'msg> -> IAsyncObservable<'msg>) (program: Elmish.Program<_,_,_,_>) =
        let mb, stream = mbStream<'msg> ()

        let subscribe _ : Cmd<'msg> =
            let sub dispatch =
                let main = async {
                    let msgObserver =
                        { new IAsyncObserver<'msg> with
                            member this.OnNextAsync x = async {
                                dispatch x
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
                Async.StartImmediate main
            Cmd.ofSub sub

        let view model _ =
            program.view model (OnNext >> mb.Post)

        { program with view = view; subscribe = subscribe }

    /// Experimental!
    /// Attach a named Reaction query to the message (Msg) stream of an Elmish program.
    /// The supplied query function will be called every time the model is updated.
    /// The returned query must be named using the `named` operator. If the query
    /// function retures a query with a new name, then the previous query will
    /// be disposed and the new query will be subscribed. This makes it possible to
    /// dynamically change the query at runtime based on the current state (Model).
    let withNamedQuery (query: 'model -> IAsyncObservable<'msg> -> INamedAsyncObservable<'msg>) (program: Elmish.Program<_,_,_,_>) =
        let mutable subscription = AsyncDisposable.Empty
        let mutable currentName = String.Empty
        let mb, stream = mbStream<'msg> ()

        let view model dispatch =
            let msgObserver =
                { new IAsyncObserver<'msg> with
                    member this.OnNextAsync x = async {
                        dispatch x
                    }
                    member this.OnErrorAsync err = async {
                        program.onError ("Reaction query error", err)
                    }
                    member this.OnCompletedAsync () = async {
                        program.onError ("Reaction query completed", Exception ())
                    }
                }

            let resubscribe (msgs: IAsyncObservable<'msg>) =
                async {
                    do! subscription.DisposeAsync ()
                    let! disposable = msgs.SubscribeAsync msgObserver
                    subscription <- disposable
                }

            let msgs = query model stream
            if msgs.Name <> currentName then
                Async.StartImmediate (resubscribe msgs)

            program.view model (OnNext >> mb.Post)

        { program with view = view }
