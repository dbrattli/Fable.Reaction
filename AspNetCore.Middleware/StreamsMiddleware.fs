namespace Elmish.Streams.AspNetCore

open System
open System.Collections.Generic
open System.Net.WebSockets
open System.Threading

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open FSharp.Control

module Middleware =
    type ConnectionId = string
    type Stream<'msg> = IAsyncObservable<'msg*ConnectionId>

    [<CLIMutable>]
    type StreamsConfig<'msg> =
        {
            /// A query for the stream of messages to a given client
            Stream: ConnectionId -> Stream<'msg> -> Stream<'msg>
            /// Encoder for serializing a message to JSON string
            Encode: 'msg -> string
            /// Decoder for deserializing a JSON string to a message
            Decode: string -> 'msg option
            /// Request path where the server will liste for websocket requests
            RequestPath: string
        }

    type GetOptions<'msg> = StreamsConfig<'msg> -> StreamsConfig<'msg>

    type ElmishStreamsMiddleware<'msg> (next: RequestDelegate, getOptions: GetOptions<'msg>)  =
        let sockets = List<WebSocket> ()
        let obv, stream = AsyncRx.subject<'msg*ConnectionId> ()

        member this.Invoke (ctx: HttpContext) =
            let loggerFactory  = ctx.RequestServices.GetService<ILoggerFactory> ()
            let logger = loggerFactory.CreateLogger("Elmish.Streams.Middleware")

            let defaultOptions = {
                Stream = fun _ msgs -> msgs
                Encode = fun msg -> ""
                Decode = fun str -> None
                RequestPath = "/ws"
            }

            let options = getOptions defaultOptions

            async {
                if ctx.Request.Path = PathString options.RequestPath then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let! webSocket =
                            ctx.WebSockets.AcceptWebSocketAsync ()
                            |> Async.AwaitTask
                        let connectionId = ctx.Connection.Id
                        logger.LogInformation ("Established WebSocket connection with ID: {ConnectionID}", connectionId)

                        sockets.Add webSocket
                        do! this.HandleWebSocket connectionId webSocket options logger
                        sockets.Remove webSocket |> ignore

                    | false -> ctx.Response.StatusCode <- 400
                else
                    return! next.Invoke ctx |> Async.AwaitTask
            } |> Async.StartAsTask

        member private this.HandleWebSocket (connectionId: ConnectionId) (webSocket: WebSocket) (options: StreamsConfig<'msg>) (logger: ILogger): Async<unit> =
            async {
                let! cancellation = Async.CancellationToken
                let mutable finished = false
                let mutable closure = WebSocketCloseStatus.NormalClosure

                let msgObserver (n: Notification<'msg*ConnectionId>) = async {
                    match n with
                    | OnNext (x, _) when not finished ->
                        let newString = options.Encode x
                        let bytes = System.Text.Encoding.UTF8.GetBytes (newString)
                        logger.LogDebug ("Sending message with {bytes} bytes", bytes.Length)
                        try
                            do! webSocket.SendAsync (new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None) |> Async.AwaitTask
                        with
                        | ex ->
                            logger.LogError("Unable to write to WebSocket, closing ...")
                            closure <- WebSocketCloseStatus.ProtocolError
                            finished <- true
                    | OnError ex ->
                        logger.LogError ("Streams stream error (OnError): {Error}", ex.ToString ())
                        closure <- WebSocketCloseStatus.InternalServerError
                        finished <- true
                    | OnCompleted ->
                        logger.LogInformation ("Streams stream completed (OnCompleted)")
                        finished <- true
                    | _ -> ()
                }

                // Stitch stream and subscribe
                let msgs = options.Stream connectionId stream
                let! subscription = msgs.SubscribeAsync msgObserver

                let buffer : byte [] = Array.zeroCreate 4096
                while not finished do
                    let rec receive messages = async {
                        let! result = webSocket.ReceiveAsync (new ArraySegment<byte>(buffer), cancellation) |> Async.AwaitTask
                        if result.CloseStatus.HasValue then
                            return Choice2Of2 result.CloseStatus.Value
                        elif result.EndOfMessage then
                            logger.LogDebug ("Received message end with {bytes} bytes", result.Count)
                            return
                                buffer.[0..result.Count] :: messages
                                |> List.rev
                                |> Array.concat
                                |> System.Text.Encoding.UTF8.GetString
                                |> Choice1Of2
                        else
                            logger.LogDebug ("Received message part with {bytes} bytes", result.Count)
                            return! receive (buffer.[0..result.Count - 1] :: messages)
                    }
                    let! response = receive []
                    match response with
                    | Choice1Of2 receiveString ->
                        let msg' = options.Decode receiveString
                        match msg' with
                        | Some msg ->
                            do! obv.OnNextAsync (msg, connectionId)
                        | None -> ()
                    | Choice2Of2 closeStatus ->
                        finished <- true
                        closure <- closeStatus

                do! subscription.DisposeAsync()

                logger.LogInformation ("Closing WebSocket with ID: {ConnectionID}", connectionId)
                try
                    do! webSocket.CloseAsync (closure, "Closing", CancellationToken.None) |> Async.AwaitTask
                with
                | _ -> ()
            }

    type IApplicationBuilder with
        member this.UseStream<'msg> (getOptions: GetOptions<'msg>) =
            this.UseMiddleware<ElmishStreamsMiddleware<'msg>> getOptions
