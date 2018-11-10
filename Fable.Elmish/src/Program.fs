namespace Reaction

open System
open System.Collections.Generic

open Elmish
open Reaction

type Query<'msg, 'name> =
    | Subscribe of IAsyncObservable<'msg>*'name
    | Queries of Query<'msg,'name> list
    | Dispose

type Query =
    /// Map a query from one message type to another.
    static member map mapper query  =
        match query with
        | Subscribe (msgs', name) ->
            Subscribe (msgs' |> AsyncRx.map mapper, name)
        | Queries q ->
            Queries [
                for query in q do
                    yield Query.map mapper query
            ]
        | Dispose -> Dispose

[<RequireQualifiedAccess>]
module Program =
    /// Attach a Reaction query to the message (Msg) stream of an Elmish program. The supplied query
    /// function will be called every time the model is updated.This makes it possible to dynamically
    /// change the query at runtime based on the current state (Model).
    let withQuery (query: 'model -> IAsyncObservable<'msg> -> Query<'msg, 'name>) (program: Elmish.Program<_,_,_,_>) =
        let subscriptions = new Dictionary<'name, IAsyncDisposable> ()
        let mb, stream = AsyncRx.mbStream<'msg> ()
        let dispatch' = OnNext >> mb.Post

        let msgObserver name dispatch =
            { new IAsyncObserver<'msg> with
                member __.OnNextAsync x = async {
                    dispatch x
                }
                member __.OnErrorAsync err = async {
                    program.onError ("Reaction query error", err)
                }
                member __.OnCompletedAsync () = async {
                    //printfn "Reaction query completed: %A" name
                    subscriptions.Remove name |> ignore
                }
            }

        let subscribe (dispatch: Dispatch<'msg>) (addMap: Map<'name, IAsyncObservable<'msg>>) =
            async {
                for KeyValue(name, msgs) in addMap do
                    //printfn "Subscribing: %A" name
                    let! disposable = msgs.SubscribeAsync (msgObserver name dispatch)
                    subscriptions.Add (name, disposable)
            }

        let dispose (dispatch: Dispatch<'msg>) (removeSet: Set<'name>) =
            async {
                for name in removeSet do
                    let disposable = subscriptions.[name]
                    do! disposable.DisposeAsync ()
                    //printfn "Disposing: %A" name
                    subscriptions.Remove name |> ignore
            }

        // The overridden view will be called on every model update (every message) so try to keep
        // it as simple and fast as possible. Re-subscribe has a penalty, but that is ok since it
        // should not happen for every message (model change).
        let view model dispatch =
            let currentKeys = Set.ofSeq subscriptions.Keys

            let query' = query model stream
            let rec loop (queries: Query<'msg, 'name> list) (keys: Set<'name>) : Map<'name, IAsyncObservable<'msg>>*Set<'name> =
                match queries with
                | query :: tail ->
                    match query with
                    | Queries queries' ->
                        loop (List.append tail queries') keys
                    | Subscribe (obs, name) ->
                        //printfn "Add: %A" name
                        let addMap, removeSet = loop tail (keys.Remove name)
                        if keys.Contains name then
                            addMap, removeSet.Remove name
                        else
                            addMap.Add (name, obs), removeSet
                    | Dispose ->
                        loop tail keys
                | [] -> Map.empty, keys

            let addMap, removeSet = loop [query'] currentKeys

            //printfn "CurrentKeys: %A" currentKeys
            //printfn "AddMap: %A" addMap
            //printfn "RemoveSet: %A" removeSet
            if addMap.Count > 0 then
                Async.StartImmediate (subscribe dispatch addMap)
            if removeSet.Count > 0 then
                Async.StartImmediate (dispose dispatch removeSet)

            program.view model dispatch'

        { program with view = view }

    /// Attach a simple Reaction query to the message (Msg) stream of an Elmish program. The
    /// supplied query function is called once by the Elmish runtime.
    let withSimpleQuery (query: IAsyncObservable<'msg> -> IAsyncObservable<'msg>) (program: Elmish.Program<_,_,_,_>) =
        let mb, stream = AsyncRx.mbStream<'msg> ()

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
