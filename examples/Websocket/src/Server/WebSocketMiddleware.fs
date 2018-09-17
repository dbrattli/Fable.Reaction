namespace WebSocketApp

module Middleware =
    open System
    open System.Text
    open System.Threading
    open System.Net.WebSockets
    open Microsoft.AspNetCore.Http

    open FSharp.Control.Tasks.ContextInsensitive

    let mutable sockets = list<WebSocket>.Empty

    let private addSocket sockets socket = socket :: sockets

    let private removeSocket sockets socket =
        sockets
        |> List.choose (fun s -> if s <> socket then Some s else None)

    let private sendMessage =
        fun (socket: WebSocket) (message: string) ->
            task {
                let buffer = Encoding.UTF8.GetBytes(message)
                let segment = new ArraySegment<byte>(buffer)

                if socket.State = WebSocketState.Open then
                    do! socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None)
                else
                    sockets <- removeSocket sockets socket
            }

    let sendMessageToSockets =
        fun message ->
            task {
                for socket in sockets do
                    try
                        do! sendMessage socket message
                    with
                        | _ -> sockets <- removeSocket sockets socket
            }

    let echo (context: HttpContext) (webSocket: WebSocket) : Async<unit> =
        async {
            printfn "echo"
            let buffer : byte [] = Array.zeroCreate 4096
            let! ct = Async.CancellationToken
            let! result = webSocket.ReceiveAsync (new ArraySegment<byte>(buffer), ct) |> Async.AwaitTask
            let mutable finished = false

            while not finished do
                printfn "looping"

                do! webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None) |> Async.AwaitTask

                let! result = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct) |> Async.AwaitTask
                finished <- result.CloseStatus.HasValue

            do! webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None) |> Async.AwaitTask
        }

    type WebSocketMiddleware(next: RequestDelegate) =
        member __.Invoke (ctx: HttpContext) =
            async {
                if ctx.Request.Path = PathString "/ws" then
                    printfn "Get ws"
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let! webSocket = ctx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                        sockets <- addSocket sockets webSocket
                        do! echo ctx webSocket

                    | false -> ctx.Response.StatusCode <- 400
                else
                    printfn "Calling next"
                    return! next.Invoke ctx
                    |> Async.AwaitTask
            } |> Async.StartAsTask
