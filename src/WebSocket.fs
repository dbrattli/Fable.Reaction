namespace Fable.Reaction

open Fable.Import.Browser

open Reaction

module WebSocket =

    exception WSError of string

    /// Websocket channel operator. Passes string items as ws messages to
    /// the server. Received ws messages will be forwarded down stream.
    /// JSON encode/decode of application messages is left to the client.
    let channel (uri: string) (source: AsyncObservable<string>) : AsyncObservable<string> =
        let subscribe (obv: Types.AsyncObserver<string>) : Async<Types.AsyncDisposable> =
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
                    let msg = (string ev.data)
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

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {'msg}.
    let msgChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> 'msg option) (source: AsyncObservable<'msg>) : AsyncObservable<'msg> =
        source
        |> map encode
        |> channel uri
        |> choose decode
