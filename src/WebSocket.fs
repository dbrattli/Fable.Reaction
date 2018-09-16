namespace Fable.Reaction

open Fable.Core.JsInterop
open Fable.Import.Browser

open Reaction

module WebSocket =

    exception WSError of string

    /// Websocket operator.
    let websocket (uri: string) (source: AsyncObservable<'msg>) : AsyncObservable<'msg> =
        let subscribe (obv: Types.AsyncObserver<'msg'>) : Async<Types.AsyncDisposable> =
            async {
                let websocket = WebSocket.Create uri
                let mutable disposable = Core.disposableEmpty

                let _obv n = async {
                    match n with
                    | OnNext msg ->
                        printfn "Sending %A" msg
                        websocket.send msg
                    | OnError ex ->
                        printfn "OnError: closing"
                        websocket.close ()
                    | OnCompleted ->
                        printfn "OnCompleted: closing"
                        websocket.close ()
                }

                let onMessage (ev : MessageEvent) =
                    let msg = ofJson<'msg'> (string ev.data)
                    Async.StartImmediate (OnNext msg |> obv)

                let onOpen _ =
                    printfn "onOpen"

                    let action = async {
                        printfn "Subscribing upstream"
                        let! disposable' = source.SubscribeAsync _obv
                        disposable <- AsyncDisposable.Unwrap disposable'
                    }

                    Async.StartImmediate action

                let onError ev =
                    printfn "onError: %A" ev

                    let ex = WSError (ev.ToString ())
                    Async.StartImmediate (OnError ex |> obv)

                let onClose ev =
                    printfn "onClose: %A" ev
                    Async.StartImmediate (obv OnCompleted)

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
