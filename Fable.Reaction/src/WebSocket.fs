namespace Fable.Reaction

open Browser
open Browser.Types
open FSharp.Control

module WebSocket =

    exception WSError of string

    /// Websocket channel operator. Passes string items as ws messages to
    /// the server. Received ws messages will be forwarded down stream.
    /// JSON encode/decode of application messages is left to the client.
    let channel (uri: string) (source: IAsyncObservable<string>) : IAsyncObservable<string> =
        let subscribe (down: IAsyncObserver<string>) : Async<IAsyncDisposable> =
            async {
                let websocket = WebSocket.Create uri
                let mutable disposable = AsyncDisposable.Empty
                let mutable disposed = false

                let cancel () = async {
                    disposed <- true
                    do! disposable.DisposeAsync ()
                    websocket.close ()
                }

                let serverObs n = async {
                    if not disposed then
                        match n with
                        | OnNext msg ->
                            try
                                websocket.send msg
                            with
                            | ex ->
                                printfn "OnNext failed, closing channel"
                                Async.StartImmediate (cancel ())
                        | OnError ex ->
                            Async.StartImmediate (cancel ())
                        | OnCompleted ->
                            Async.StartImmediate (cancel ())
                }

                let onMessage (ev : MessageEvent) =
                    let msg = (string ev.data)
                    Async.StartImmediate (down.OnNextAsync msg)

                let onOpen _ =
                    let action = async {
                        let! disposable' = source.SubscribeAsync serverObs
                        disposable <- disposable'
                    }
                    Async.StartImmediate action

                let onError ev =
                    let ex = WSError (ev.ToString ())
                    Async.StartImmediate (async {
                        do! down.OnErrorAsync ex
                        do! cancel ()
                    })

                let onClose ev =
                    Async.StartImmediate (async {
                        do! down.OnCompletedAsync ()
                        do! cancel ()
                    })

                websocket.onmessage <- onMessage
                websocket.onclose <- onClose
                websocket.onopen <- onOpen
                websocket.onerror <- onError

                return AsyncDisposable.Create cancel
            }

        AsyncRx.create subscribe

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {'msg}.
    let msgChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> 'msg option) (source: IAsyncObservable<'msg>) : IAsyncObservable<'msg> =
        source
        |> AsyncRx.map encode
        |> channel uri
        |> AsyncRx.choose decode

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {Result<'msg, exn>}.
    let msgResultChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> Result<'msg, exn>) (source: IAsyncObservable<'msg>) : IAsyncObservable<Result<'msg, exn>> =
        source
        |> AsyncRx.map encode
        |> channel uri
        |> AsyncRx.map decode
