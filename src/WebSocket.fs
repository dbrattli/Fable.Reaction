namespace Fable.Reaction

open Fable.Import.Browser

open Reaction
open Reaction.AsyncObservable

module WebSocket =

    exception WSError of string

    /// Websocket channel operator. Passes string items as ws messages to
    /// the server. Received ws messages will be forwarded down stream.
    /// JSON encode/decode of application messages is left to the client.
    let channel (uri: string) (source: IAsyncObservable<string>) : IAsyncObservable<string> =
        let subscribe (obv: IAsyncObserver<string>) : Async<IAsyncDisposable> =
            async {
                let websocket = WebSocket.Create uri
                let mutable disposable = AsyncDisposable.Empty

                let _obv n = async {
                    match n with
                    | OnNext msg ->
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
                    Async.StartImmediate (obv.OnNextAsync msg)

                let onOpen _ =
                    let action = async {
                        let! disposable' = source.SubscribeAsync _obv
                        disposable <- disposable'
                    }

                    Async.StartImmediate action

                let onError ev =
                    let ex = WSError (ev.ToString ())
                    Async.StartImmediate (obv.OnErrorAsync ex)

                let onClose ev =
                    Async.StartImmediate (obv.OnCompletedAsync ())

                websocket.onmessage <- onMessage
                websocket.onclose <- onClose
                websocket.onopen <- onOpen
                websocket.onerror <- onError

                let cancel () = async {
                    do! disposable.DisposeAsync ()
                    websocket.close ()
                }
                return AsyncDisposable.Create cancel
            }

        AsyncObservable.create subscribe

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {'msg}.
    let msgChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> 'msg option) (source: IAsyncObservable<'msg>) : IAsyncObservable<'msg> =
        source
        |> map encode
        |> channel uri
        |> choose decode

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {Result<'msg, exn>}.
    let msgResultChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> Result<'msg, exn>) (source: IAsyncObservable<'msg>) : IAsyncObservable<Result<'msg, exn>> =
        source
        |> map encode
        |> channel uri
        |> map decode
