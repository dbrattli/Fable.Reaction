namespace Fable.Reaction

open System

open Elmish
open Reaction
open Reaction.AsyncObservable
open Reaction.Streams

[<RequireQualifiedAccess>]
module Program =

    /// Attach a named Reaction query to the message (Msg) stream of an Elmish program. The supplied
    /// query function will be called every time the model is updated. The returned query is tupled
    /// with a key that identifies the query. If a new key is returned, then the previous query will
    /// be disposed and the new query will be subscribed. This makes it possible to dynamically
    /// change the query at runtime based on the current state (Model).
    let withQuery (query: 'model -> IAsyncObservable<'msg> -> IAsyncObservable<'msg>*'key) (program: Elmish.Program<_,_,_,_>) =
        let mutable subscription = AsyncDisposable.Empty
        let mutable currentKey = Unchecked.defaultof<'key>
        let mb, stream = mbStream<'msg> ()
        let dispatch' = OnNext >> mb.Post

        let msgObserver dispatch =
            { new IAsyncObserver<'msg> with
                member __.OnNextAsync x = async {
                    dispatch x
                }
                member __.OnErrorAsync err = async {
                    program.onError ("Reaction query error", err)
                }
                member __.OnCompletedAsync () = async {
                    program.onError ("Reaction query completed", System.Exception ())
                }
            }

        let resubscribe (msgs: IAsyncObservable<'msg>) (dispatch: Dispatch<'msg>) (key: 'key) =
            async {
                do! subscription.DisposeAsync ()
                currentKey <- key
                let! disposable = msgs.SubscribeAsync (msgObserver dispatch)
                subscription <- disposable
            }

        // The overridden view will be called on every model update (every message) so try to keep
        // it as simple and fast as possible. Re-subscribe has a penalty, but that is ok since it
        // should not happen for every message (model change).
        let view model dispatch =
            let msgs, key = query model stream
            if key <> currentKey then
                Async.StartImmediate (resubscribe msgs dispatch key)
            program.view model dispatch'

        { program with view = view }

    /// Helper function to call a sub-query for a page or component. It will help with extracting
    /// the sub-message using `AsyncObservable.choose` and also wrapping back to msg using
    /// `AsyncObservable.map`. Returns tuple of (subquery, key) i.e. `IAsyncObservable<'msg>, 'key`.
    let withSubQuery subquery submodel msgs wrapMsg unwrapMsg : IAsyncObservable<_> * string =
        let msgs', name = subquery submodel (msgs |> AsyncObservable.choose unwrapMsg)
        (msgs' |> AsyncObservable.map wrapMsg, name)

    /// Attach a simple Reaction query to the message (Msg) stream of an Elmish program. The
    /// supplied query function is called once by the Elmish runtime.
    let withSimpleQuery (query: IAsyncObservable<'msg> -> IAsyncObservable<'msg>) (program: Elmish.Program<_,_,_,_>) =
        let mb, stream = mbStream<'msg> ()

        let subscribe _ : Cmd<'msg> =
            let sub dispatch =
                let main = async {
                    let msgObserver =
                        { new IAsyncObserver<'msg> with
                            member __.OnNextAsync x = async {
                                dispatch x
                            }
                            member __.OnErrorAsync err = async {
                                program.onError ("Reaction query error", err)
                            }
                            member __.OnCompletedAsync () = async {
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

