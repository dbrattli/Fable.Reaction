namespace Reaction

open System.Threading

open Fable.Core
open Browser
open Browser.Types


/// AsyncRx Extensions
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncRx =
    /// Returns an observable that produces a notification when the
    /// promise resolves. The observable will also complete after
    /// producing an event.
    let ofPromise (pr: Fable.Core.JS.Promise<_>) =
        Create.ofAsyncWorker(fun obv _ -> async {
            try
                let! result = Async.AwaitPromise pr
                do! obv.OnNextAsync result
                do! obv.OnCompletedAsync ()
            with
            | ex ->
                do! obv.OnErrorAsync ex
        })

    /// Returns an async observable of Window events.
    let ofEvent<'ev> event : IAsyncObservable<'ev> =
        let cts = new CancellationTokenSource()

        let subscribe (obv: IAsyncObserver<'ev'>) : Async<IAsyncDisposable> =
            async {
                let mb = MailboxProcessor.Start(fun inbox ->
                    let rec messageLoop _ = async {
                        let! ev = inbox.Receive ()
                        do! obv.OnNextAsync ev

                        return! messageLoop ()
                    }
                    messageLoop ()
                , cts.Token)

                window.addEventListener (event, unbox mb.Post)
                let cancel () = async {
                    cts.Cancel ()
                    window.removeEventListener (event, unbox mb.Post)
                }
                return AsyncDisposable.Create cancel
            }

        AsyncRx.create subscribe

    /// Returns an async observable of mouse events.
    let ofMouseMove () : IAsyncObservable<MouseEvent> =
        ofEvent "mousemove"

    /// Websocket channel operator. Passes string items as ws messages to
    /// the server. Received ws messages will be forwarded down stream.
    /// JSON encode/decode of application messages is left to the client.
    let inline channel (uri: string) (source: IAsyncObservable<string>) : IAsyncObservable<string> =
        Reaction.WebSocket.channel uri source

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {'msg}.
    let inline msgChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> 'msg option) (source: IAsyncObservable<'msg>) : IAsyncObservable<'msg> =
        Reaction.WebSocket.msgChannel uri encode decode source

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {Result<'msg, exn>}.
    let msgResultChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> Result<'msg, exn>) (source: IAsyncObservable<'msg>) : IAsyncObservable<Result<'msg, exn>> =
        Reaction.WebSocket.msgResultChannel uri encode decode source

    /// Turn the observable into a named stream
    let inline toStream (name: 'name) (source: IAsyncObservable<'a>) : Stream<'a, 'name> =
        Stream [source, name]
