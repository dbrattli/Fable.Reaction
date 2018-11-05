namespace Fable.Reaction

open System
open System.Collections.Generic

open Elmish
open Reaction
open Reaction.AsyncObservable
open Reaction.Streams

[<RequireQualifiedAccess>]
module Program =

    /// Attach a Reaction query to the message (Msg) stream of an Elmish program. The supplied query
    /// function will be called every time the model is updated. The returned query is tupled with a
    /// key that identifies the query. If a new key is returned, then the previous query will be
    /// disposed and the new query will be subscribed. This makes it possible to dynamically change
    /// the query at runtime based on the current state (Model). Returns tuple of (query, key) i.e.
    /// `IAsyncObservable<'msg>*'key`.
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

    let withQueries (query: 'model -> IAsyncObservable<'msg> -> (IAsyncObservable<'msg>*'key) list) (program: Elmish.Program<_,_,_,_>) =
        let subscriptions = new Dictionary<'key, IAsyncDisposable> ()
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

        let subscribe (map: Map<'key, IAsyncObservable<'msg>>) (dispatch: Dispatch<'msg>) (keys: Set<'key>) =
            async {
                for key in keys do
                    let msgs = map.[key]
                    let! disposable = msgs.SubscribeAsync (msgObserver dispatch)
                    subscriptions.Add (key, disposable)
            }

        let dispose (dispatch: Dispatch<'msg>) (keys: Set<'key>) =
            async {
                for key in keys do
                    let disposable = subscriptions.[key]
                    do! disposable.DisposeAsync ()
                    subscriptions.Remove key |> ignore
            }

        // The overridden view will be called on every model update (every message) so try to keep
        // it as simple and fast as possible. Re-subscribe has a penalty, but that is ok since it
        // should not happen for every message (model change).
        let view model dispatch =
            let result = query model stream

            let keySet = result |> List.map snd |> Set.ofList
            let currentKeys = Set.ofSeq subscriptions.Keys
            let addSet = Set.difference keySet currentKeys
            let removeSet = Set.difference currentKeys keySet

            if addSet.Count > 0 then
                let resultMap = result |> List.map (fun (x, y) -> (y, x)) |> Map.ofList
                Async.StartImmediate (subscribe resultMap dispatch addSet)
            if removeSet.Count > 0 then
                Async.StartImmediate (dispose dispatch addSet)

            program.view model dispatch'

        { program with view = view }

    /// Helper function to call a sub-query for a page or component. It will help with extracting
    /// the sub-message using `AsyncObservable.choose` and also wrapping the stream of sub-messages
    /// back to a stream of messages using `AsyncObservable.map`. Returns tuple of (subquery, key)
    /// i.e. `IAsyncObservable<'msg>*'key`.
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

