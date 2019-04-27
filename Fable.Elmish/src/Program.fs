namespace Elmish.Streams

open System.Collections.Generic

open Elmish
open Elmish.Streams.Error
open FSharp.Control


[<RequireQualifiedAccess>]
module Program =
    #if DEBUG
    let debug (text: string, o: #obj) = printfn "%s %A" text o
    #else
    let debug (text: string, o: #obj) = ()
    #endif

    /// **Description**
    /// Transforms the Elmish message stream. The supplied `stream` function takes the model and a
    /// named message stream, and returns a (possibly) transformed named message stream. The
    /// `stream` function will be called each time the model is updated. This makes it possible to
    /// dynamically change the stream handling at runtime based on the current model. If the name
    /// of a stream changes, the old stream will be disposed and the new stream will be subscribed
    /// and active.
    ///
    /// **Parameters**
    ///   * `stream` - A stream modifying function of type `'model -> Stream<'msg,'name> -> Stream<'msg,'name>`.
    ///   * `initialName` - Initial name of stream e.g "msgs". Type of `'name` must support comparison.
    ///   * `program` - Elmish program of type `Program<'a,'model,'msg,'b>`
    ///
    /// **Output Type**
    ///   * `Program<'a,'model,'msg,'b>`
    let withStream (stream: 'model -> Stream<'msg, 'name> -> Stream<'msg, 'name>) (initialName: 'name) (program: Elmish.Program<_,_,_,_>) =
        let subscriptions = new Dictionary<'name, IAsyncDisposable> ()
        let mb, obs = AsyncRx.mbStream<'msg> ()
        let dispatch' = OnNext >> mb.Post

        let msgObserver name dispatch =
            { new IAsyncObserver<'msg> with
                member __.OnNextAsync x = async {
                    dispatch x
                }
                member __.OnErrorAsync err = async {
                    onError ("[Elmish.Streams] Stream error", err)
                }
                member __.OnCompletedAsync () = async {
                    debug ("[Elmish.Streams] Stream completed:", name)
                    subscriptions.Remove name |> ignore
                }
            }

        let subscribe (dispatch: Dispatch<'msg>) (adds: Subscription<'msg, 'name> list) =
            async {
                for msgs, name in adds do
                    debug ("[Elmish.Streams] Subscribing stream:", name)
                    let! disposable = msgs.SubscribeAsync (msgObserver name dispatch)
                    subscriptions.Add (name, disposable)
            }

        let dispose (dispatch: Dispatch<'msg>) (removes: Set<'name>) =
            async {
                for name in removes do
                    do! subscriptions.[name].DisposeAsync ()
                    debug ("[Elmish.Streams] Disposing stream:", name)
                    subscriptions.Remove name |> ignore
            }

        // The overridden view will be called on every model update (every message) so try to keep
        // it as simple and fast as possible. Re-subscribe has a penalty, but that is ok since it
        // should not happen for every message (model change).
        let view userView model dispatch =
            let currentKeys = Set.ofSeq subscriptions.Keys

            let (Stream streams) = Stream [obs, initialName] |> stream model

            let removes = Set.difference currentKeys (streams |> List.map (fun (xs, name) -> name) |> Set.ofList)
            let adds = streams |> List.filter (fun (xs, name) -> not (currentKeys.Contains name))

            if adds.Length > 0 then
                Async.StartImmediate (subscribe dispatch adds)
            if removes.Count > 0 then
                Async.StartImmediate (dispose dispatch removes)

            userView model dispatch'

        program
        |> Program.map id id view id id
