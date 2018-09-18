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

module Middleware =
    type ConnectionId = string
    type Query<'msg> = ConnectionId -> AsyncObservable<'msg*ConnectionId> -> AsyncObservable<'msg*ConnectionId>

    [<CLIMutable>]
    type ReactionConfig<'msg> =
        {
            /// A query for the stream of all messages
            QueryAll: AsyncObservable<'msg*ConnectionId> -> AsyncObservable<'msg*ConnectionId>
            /// A query for stream of messages to a given client
            Query: ConnectionId -> AsyncObservable<'msg*ConnectionId> -> AsyncObservable<'msg*ConnectionId>
            /// Encoder for serializing a message to JSON string
            Encode: 'msg -> string
            /// Decoder for deserializing a JSON string to a message
            Decode: string -> 'msg option
            /// Request path where the server will liste for websocket requests
            RequestPath: string
        }

    type GetConfig<'msg> = ReactionConfig<'msg> -> ReactionConfig<'msg>

    type ReactionMiddleware<'msg> (next: RequestDelegate, getConfig: GetConfig<'msg>)  =
        let sockets = List<WebSocket> ()
        let obvAll, streamAll = stream<'msg*ConnectionId> ()
        let obv, stream = stream<'msg*ConnectionId> ()
        let mutable subscription : AsyncDisposable option = None

        member this.Invoke (ctx: HttpContext) =
            let serializer : IJsonSerializer = ctx.RequestServices.GetService<IJsonSerializer> ()

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

            let options = getConfig defaultOptions

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
                        printfn "Connection ID: %A" connectionId

                        sockets.Add webSocket
                        do! this.Reaction connectionId webSocket options

                    | false -> ctx.Response.StatusCode <- 400
                else
                    return! next.Invoke ctx |> Async.AwaitTask
            } |> Async.StartAsTask

        member private this.Reaction (socketId: ConnectionId) (webSocket: WebSocket) (options: ReactionConfig<'msg>) : Async<unit> =
            async {
                let buffer : byte [] = Array.zeroCreate 4096
                let! ct = Async.CancellationToken
                let! result = webSocket.ReceiveAsync (new ArraySegment<byte> (buffer), ct) |> Async.AwaitTask
                let mutable finished = false

                let msgObserver (n: Notification<'msg*ConnectionId>) = async {
                    match n with
                    | OnNext (x, _) ->
                        let newString = options.Encode x
                        let bytes = System.Text.Encoding.UTF8.GetBytes (newString)
                        do! webSocket.SendAsync (new ArraySegment<byte>(bytes), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None) |> Async.AwaitTask
                    | OnError ex ->
                        printf "Got OnError: %A" ex
                        finished <- true
                    | OnCompleted ->
                        printf "Got OnCompleted"
                        finished <- true
                }

                let msgs = options.Query socketId stream
                do! msgs.RunAsync msgObserver

                while not finished do

                    let! result = webSocket.ReceiveAsync (new ArraySegment<byte>(buffer), ct) |> Async.AwaitTask
                    finished <- result.CloseStatus.HasValue

                    if not finished then
                        let receiveString = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count)
                        let msg' = options.Decode receiveString
                        match msg' with
                        | Some msg ->
                            do! obvAll.OnNextAsync (msg, socketId)
                        | None -> ()

                do! webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None) |> Async.AwaitTask
                sockets.Remove webSocket |> ignore
            }


    type IApplicationBuilder with
        member this.UseReaction<'msg> (getConfig: GetConfig<'msg>) =
            this.UseMiddleware<ReactionMiddleware<'msg>> getConfig
