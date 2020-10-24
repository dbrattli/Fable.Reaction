namespace Fable.Reaction

open System.Threading

open Browser
open Browser.Types

open Fable.Core.JsInterop
open Fetch

open FSharp.Control

type TaggedStream<'msg, 'tag> = IAsyncObservable<'msg> * 'tag

/// AsyncRx Extensions
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncRx =
    /// Returns an observable that produces a notification when the
    /// promise resolves. The observable will also complete after
    /// producing an event.
    let ofPromise (pr: Fable.Core.JS.Promise<_>) =
        QueryBuilderExtension.ofPromise pr

    let msgRequest (url:string) (encode: 'msg -> string) (decode: string -> 'msg option) (msgs: IAsyncObservable<'msg>) =
        asyncRx {
            let! body = msgs |> AsyncRx.map encode

            let init = [
                Body !!body
                Method Fetch.Types.HttpMethod.POST
                Fetch.requestHeaders [
                    HttpRequestHeaders.ContentType "application/json"
                ]
            ]

            let! response = GlobalFetch.fetch(RequestInfo.Url url, Fetch.requestProps init)
            yield! response.text ()
        }

    /// Returns an async observable of Window events.
    let ofEvent<'ev> event : IAsyncObservable<'ev> =
        let cts = new CancellationTokenSource ()

        let subscribe (obv: IAsyncObserver<'ev>) : Async<IAsyncRxDisposable> =
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
    let ofMouseMove () : IAsyncObservable<MouseEvent> = ofEvent "mousemove"

    /// Debounces an async observable sequence to the animation frame rate.
    let requestAnimationFrame<'msg> (source: IAsyncObservable<'msg>) : IAsyncObservable<'msg> =
        let subscribeAsync (aobv : IAsyncObserver<'msg>) : Async<IAsyncRxDisposable> =
            let mutable lastRequest = None

            { new IAsyncObserver<'msg> with
                member __.OnNextAsync x = async {
                    match lastRequest with
                    | Some r -> window.cancelAnimationFrame r
                    | _ -> ()

                    lastRequest <-
                        window.requestAnimationFrame (fun _ ->
                            aobv.OnNextAsync x |> Async.StartImmediate
                        ) |> Some
                }
                member __.OnErrorAsync err = aobv.OnErrorAsync err
                member __.OnCompletedAsync () = aobv.OnCompletedAsync ()
            }
            |> source.SubscribeAsync
        { new IAsyncObservable<'msg> with member __.SubscribeAsync o = subscribeAsync o }

    /// Websocket channel operator. Passes string items as ws messages to
    /// the server. Received ws messages will be forwarded down stream.
    /// JSON encode/decode of application messages is left to the client.
    let inline channel (uri: string) (source: IAsyncObservable<string>) : IAsyncObservable<string> =
        Fable.Reaction.WebSocket.channel uri source

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {'msg}.
    let inline msgChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> 'msg option) (source: IAsyncObservable<'msg>) : IAsyncObservable<'msg> =
        Fable.Reaction.WebSocket.msgChannel uri encode decode source

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {Result<'msg, exn>}.
    let msgResultChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> Result<'msg, exn>) (source: IAsyncObservable<'msg>) : IAsyncObservable<Result<'msg, exn>> =
        Fable.Reaction.WebSocket.msgResultChannel uri encode decode source

    /// Tags an async observable with an identifier.
    let tag<'msg, 'tag> (tag : 'tag) (obs: IAsyncObservable<'msg>) : TaggedStream<'msg, 'tag> = obs, tag
