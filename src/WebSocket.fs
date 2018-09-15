namespace Fable.Reaction.WebSocket

open Fable.Core.JsInterop
open Fable.Import.Browser

open Reaction

module WebSocket =

    exception Error of string

    /// Websocket operator.
    let websocket (uri: string) (source: AsyncObservable<'msg>) : AsyncObservable<'msg> =
        let subscribe (obv: Types.AsyncObserver<'msg'>) : Async<Types.AsyncDisposable> =
            async {
                let websocket = WebSocket.Create uri
                let mutable disposable = Core.disposableEmpty

                let _obv n = async {
                    match n with
                    | OnNext x -> websocket.send x
                    | OnError ex ->
                        websocket.close ()
                    | OnCompleted ->
                        websocket.close ()
                }

                let onMessage (ev : MessageEvent) =
                    let msg = ofJson<'msg'> (string ev.data)
                    Async.RunSynchronously (OnNext msg |> obv)

                let onOpen _ =
                    let action = async {
                        let! disposable' = source.SubscribeAsync _obv
                        disposable <- AsyncDisposable.Unwrap disposable'
                    }

                    Async.StartImmediate action

                let onError ev =
                    let ex = Error (ev.ToString ())
                    Async.RunSynchronously (OnError ex |> obv)

                let onClose ev =
                    Async.RunSynchronously (obv OnCompleted)

                websocket.onmessage <- onMessage
                websocket.onclose <- onClose
                websocket.onopen <- onOpen
                websocket.onerror <- onError

                let cancel () = async {
                    do! disposable ()
                }
                return cancel
            }

        AsyncObservable subscribe
