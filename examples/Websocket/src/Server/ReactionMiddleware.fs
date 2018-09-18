namespace WebSocketApp

open System
open System.Net.WebSockets
open System.Threading
open System.Text
open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

open Reaction

module Middleware =
    [<CLIMutable>]
    type ReactionConfig<'msg> =
        {
            Query: AsyncObservable<'msg> -> AsyncObservable<'msg>
            Encode: 'msg -> string
            Decode: string -> 'msg option
        }


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

    let reaction (context: HttpContext) (webSocket: WebSocket) (query: AsyncObservable<'msg> -> AsyncObservable<'msg>) (encode: 'msg -> string) (decode: string -> 'msg option) : Async<unit> =
        let obv, stream = stream<'msg> ()
        async {
            let buffer : byte [] = Array.zeroCreate 4096
            let! ct = Async.CancellationToken
            let! result = webSocket.ReceiveAsync (new ArraySegment<byte>(buffer), ct) |> Async.AwaitTask
            let mutable finished = false

            let msgObserver n = async {
                match n with
                | OnNext x ->
                    let newString = encode x
                    let bytes = System.Text.Encoding.UTF8.GetBytes (newString)
                    do! webSocket.SendAsync (new ArraySegment<byte>(bytes), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None) |> Async.AwaitTask

                | OnError ex -> ()
                | OnCompleted -> ()
            }

            let msgs = query stream
            do! msgs.RunAsync msgObserver

            while not finished do

                let! result = webSocket.ReceiveAsync (new ArraySegment<byte>(buffer), ct) |> Async.AwaitTask
                finished <- result.CloseStatus.HasValue

                if not finished then
                    let receiveString = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count)
                    let msg' = decode receiveString
                    match msg' with
                    | Some msg ->
                        do! obv.OnNextAsync msg
                    | None -> ()

            do! webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None) |> Async.AwaitTask
        }

    type ReactionMiddleware<'msg> (next: RequestDelegate, getOptions: unit -> ReactionConfig<'msg>)  =
        member __.Invoke (ctx: HttpContext) =
            let options = getOptions ()
            async {
                if ctx.Request.Path = PathString "/ws" then
                    printfn "Get ws"
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let! webSocket = ctx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                        sockets <- addSocket sockets webSocket
                        do! reaction ctx webSocket options.Query options.Encode options.Decode

                    | false -> ctx.Response.StatusCode <- 400
                else
                    printfn "Calling next"
                    return! next.Invoke ctx
                    |> Async.AwaitTask
            } |> Async.StartAsTask

    type IApplicationBuilder with
        member this.UseReaction<'msg> (getOptions: unit -> ReactionConfig<'msg>) =
            this.UseMiddleware<ReactionMiddleware<'msg>> getOptions
