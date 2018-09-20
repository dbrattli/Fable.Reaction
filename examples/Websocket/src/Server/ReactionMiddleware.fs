namespace WebSocketApp

open System
open System.Net.WebSockets
open System.Threading
open System.Collections.Generic

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Giraffe.Serialization

open Reaction
open Microsoft.Extensions.Logging

module Middleware =
    type ConnectionId = string
    type Query<'msg> = ConnectionId -> AsyncObservable<'msg*ConnectionId> -> AsyncObservable<'msg*ConnectionId>

    [<CLIMutable>]
    type ReactionConfig<'msg> =
        {
            /// A query for the stream of all messages
            QueryAll: AsyncObservable<'msg*ConnectionId> -> AsyncObservable<'msg*ConnectionId>
            /// A query for the stream of messages to a given client
            Query: ConnectionId -> AsyncObservable<'msg*ConnectionId> -> AsyncObservable<'msg*ConnectionId>
            /// Encoder for serializing a message to JSON string
            Encode: 'msg -> string
            /// Decoder for deserializing a JSON string to a message
            Decode: string -> 'msg option
            /// Request path where the server will liste for websocket requests
            RequestPath: string
        }

    type GetOptions<'msg> = ReactionConfig<'msg> -> ReactionConfig<'msg>

    type ReactionMiddleware<'msg> (next: RequestDelegate, getOptions: GetOptions<'msg>)  =
        let sockets = List<WebSocket> ()
        let obvAll, streamAll = stream<'msg*ConnectionId> ()
        let obv, stream = stream<'msg*ConnectionId> ()
        let mutable subscription : AsyncDisposable option = None

        member this.Invoke (ctx: HttpContext) =
            let serializer = ctx.RequestServices.GetService<IJsonSerializer> ()
            let loggerFactory  = ctx.RequestServices.GetService<ILoggerFactory> ()
            let logger = loggerFactory.CreateLogger("Reaction.Middleware")

            let defaultOptions = {
                QueryAll = fun msgs -> msgs
                Query = fun _ msgs -> msgs
                Encode = fun msg ->
                    serializer.Serialize msg

                Decode = fun str ->
                    let msg =
                        try
                            serializer.Deserialize<'msg> str |> Some
                        with
                        | _ -> None
                    msg
                RequestPath = "/ws"
            }

            let options = getOptions defaultOptions

            async {
                // One time setup of the broadcast query (all sockets)
                if subscription.IsNone then
                    let queryAll = options.QueryAll streamAll
                    let! disposable = queryAll.SubscribeAsync obv
                    subscription <- Some disposable

                if ctx.Request.Path = PathString options.RequestPath then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let! webSocket = ctx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                        let connectionId = ctx.Connection.Id
                        logger.LogInformation ("Established WebSocket connection with ID: {ConnectionID}", connectionId)

                        sockets.Add webSocket
                        do! this.Reaction connectionId webSocket options logger

                    | false -> ctx.Response.StatusCode <- 400
                else
                    return! next.Invoke ctx |> Async.AwaitTask
            } |> Async.StartAsTask

        member private this.Reaction (connectionId: ConnectionId) (webSocket: WebSocket) (options: ReactionConfig<'msg>) (logger: ILogger): Async<unit> =
            async {
                let buffer : byte [] = Array.zeroCreate 4096
                let! ct = Async.CancellationToken
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
                        logger.LogError ("Reaction stream error (OnError): {Error}", ex.ToString ())
                        closure <- WebSocketCloseStatus.InternalServerError
                        finished <- true
                    | OnCompleted ->
                        logger.LogInformation ("Reaction stream completed (OnCompleted)")
                        finished <- true
                    | _ -> ()
                }

                let msgs = options.Query connectionId stream
                do! msgs.RunAsync msgObserver

                while not finished do
                    let! result = webSocket.ReceiveAsync (new ArraySegment<byte>(buffer), ct) |> Async.AwaitTask
                    finished <- result.CloseStatus.HasValue

                    if not finished then
                        logger.LogDebug ("Received message with {bytes} bytes", result.Count)
                        let receiveString = System.Text.Encoding.UTF8.GetString (buffer, 0, result.Count)
                        let msg' = options.Decode receiveString
                        match msg' with
                        | Some msg ->
                            do! obvAll.OnNextAsync (msg, connectionId)
                        | None -> ()
                    else
                        closure <- result.CloseStatus.Value

                logger.LogInformation ("Closing WebSocket with ID: {ConnectionID}", connectionId)
                try
                    do! webSocket.CloseAsync (closure, "Closing", CancellationToken.None) |> Async.AwaitTask
                with
                | _ -> ()
                sockets.Remove webSocket |> ignore
            }

    type IApplicationBuilder with
        member this.UseReaction<'msg> (getOptions: GetOptions<'msg>) =
            this.UseMiddleware<ReactionMiddleware<'msg>> getOptions
