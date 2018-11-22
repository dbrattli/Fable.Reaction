namespace Reaction

open Fable.Core
open Fable.Import.Browser


[<RequireQualifiedAccess>]
type Stream =
    /// Map stream from one message type to another.
    static member map (mapper: 'msgIn -> 'msgOut) (stream: Stream<'msgIn, 'name>) :Stream<'msgOut, 'name> =
        match stream with
        | Stream (msgs', name) ->
            Stream.Stream (msgs' |> AsyncRx.map mapper, name)
        | Streams q ->
            Streams [
                for query in q do
                    yield Stream.map mapper query
            ]
        | Dispose -> Dispose

    /// Applies the given chooser function to each element of the stream and
    /// returns the stream comprised of the results for each element where the
    /// function returns Some with some value.
    static member choose (chooser: 'msgIn -> 'msgOut option) (stream: Stream<'msgIn, 'name>) : Stream<'msgOut, 'name> =
        match stream with
        | Stream (msgs', name) ->
            Stream.Stream (msgs' |> AsyncRx.choose chooser, name)
        | Streams q ->
            Streams [
                for query in q do
                    yield Stream.choose chooser query
            ]
        | Dispose -> Dispose

    /// Selects the stream wit the given name and applies the given chooser
    /// function to each element of the stream and returns the stream comprised
    /// of the results for each element where the function returns Some with
    /// some value.
    static member chooseNamed (name: 'name) (chooser: 'msgIn -> 'msgOut option) (stream: Stream<'msgIn, 'name>) : Stream<'msgOut, 'name> =
        match stream with
        | Stream (xs, name) when name=name ->
            Stream.Stream (xs |> AsyncRx.choose chooser, name)
        | Streams xss ->
            match xss with
            | xs :: tail ->
                match xs with
                | Stream (xs, name) when name=name ->
                    Stream.Stream (xs |> AsyncRx.choose chooser, name)
                | Streams xss ->
                    let xs = Stream.chooseNamed name chooser (Stream.Streams xss)
                    match xs with
                    | Dispose -> Stream.chooseNamed name chooser (Stream.Streams tail)
                    | _ -> xs
                | _ -> Stream.chooseNamed name chooser (Stream.Streams tail)
            | [] -> Dispose
        | _ -> Dispose

/// Named message stream. Can be a single Stream, a collection of Streams or Dispose to remove a
/// stream.
and Stream<'msg, 'name> =
    /// Named message stream
    | Stream of IAsyncObservable<'msg>*'name
    /// Collection of named streams
    | Streams of Stream<'msg, 'name> list
    /// Dispose message stream
    | Dispose
    interface IAsyncObservable<'msg> with
        member this.SubscribeAsync obv =
            let rec flatten streams =
                [
                    for stream in streams do
                        match stream with
                        | Stream (xs, _) ->
                            yield xs
                        | Streams xss ->
                            yield! flatten xss
                        | _ -> ()
                ]
            async {
                match this with
                | Stream (xs, _) ->
                    return! xs.SubscribeAsync obv
                | Streams xss ->
                    let xs = flatten xss |> AsyncRx.mergeSeq
                    return! xs.SubscribeAsync obv
                | _ ->
                    return AsyncDisposable.Empty
            }

/// Extra Reaction operators that may be used from Fable
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncRx =
    /// Returns an observable that produces a notification when the
    /// promise resolves. The observable will also complete after
    /// producing an event.
    let ofPromise (pr: Fable.Import.JS.Promise<_>) =
        Create.ofAsyncWorker(fun obv _ -> async {
            try
                let! result = Async.AwaitPromise pr
                do! obv.OnNextAsync result
                do! obv.OnCompletedAsync ()
            with
            | ex ->
                do! obv.OnErrorAsync ex
        })

    /// Returns an async observable of mouse events.
    let ofMouseMove () : IAsyncObservable<Fable.Import.Browser.MouseEvent> =
        let subscribe (obv: IAsyncObserver<Fable.Import.Browser.MouseEvent>) : Async<IAsyncDisposable> =
            async {
                let onMouseMove (ev: Fable.Import.Browser.MouseEvent) =
                    async {
                        do! obv.OnNextAsync ev
                    } |> Async.StartImmediate

                window.addEventListener_mousemove onMouseMove
                let cancel () = async {
                    window.removeEventListener ("mousemove", unbox onMouseMove)
                }
                return AsyncDisposable.Create cancel
            }

        AsyncRx.create subscribe

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

    /// Wrap observable as a named stream
    let inline asStream (name: 'name) (source: IAsyncObservable<'a>) : Stream<'a, 'name> =
        Stream (source, name)
