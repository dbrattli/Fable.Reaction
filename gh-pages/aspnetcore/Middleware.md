# Reaction Middleware for AspNet Core

The Reaction Middleware for AspNet Core is an experimental support for server side WebSocket message handling.

## Install

```sh
> paket add Reaction.AspNet.Middleware --project <project>
```

## Getting Started

You add the Reaction middleware to Giraffe using the `UseReaction<'msg>` method on the `IApplicationBuilder` in the `configureApp` function of the Giraffe setup.

```fs
let query (connectionId: ConnectionId) (msgs: IAsyncObservable<Msg*ConnectionId>) : IAsyncObservable<Msg*ConnectionId> =
    msgs
    |> filter (fun (msg, cId) -> cId = connectionId)

let configureApp (app : IApplicationBuilder) =
    app.UseWebSockets()
       .UseReaction<Msg>(fun options ->
       { options with
           Query = query
           Encode = Msg.Encode
           Decode = Msg.Decode
       })
       .UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp
```

## Options

Options may be overrided by `UseReaction`.

```fs
[<CLIMutable>]
type ReactionConfig<'msg> =
    {
        QueryAll: IAsyncObservable<'msg*ConnectionId> -> IAsyncObservable<'msg*ConnectionId>
        Query: ConnectionId -> IAsyncObservable<'msg*ConnectionId> -> IAsyncObservable<'msg*ConnectionId>
        Encode: 'msg -> string
        Decode: string -> 'msg option
        RequestPath: string
    }
```

### Query

A query for the stream of messages to a given client. The type of the query is `ConnectionId -> IAsyncObservable<'msg*ConnectionId> -> IAsyncObservable<'msg*ConnectionId>`.

The `query` function takes a `connectionId` and an `AsyncObservable<'msg*ConnectionId>`. The `connectionId` is the id for a particular clients connection. Messages that leaves in the returned observable will only go to that particular client. Notifications will be a tuple `'msg*ConnectionId` where `'msg` is the message being handled and the `connectionId` is the client connection where this message was received.

Thus this query will only send messages back to the same client that sent them. Messages for other clients will be filtered.

```fs
let query (connectionId: ConnectionId) (msgs: IAsyncObservable<Msg*ConnectionId>) : IAsyncObservable<Msg*ConnectionId> =
    msgs
    |> AsyncRx.filter (fun (msg, cId) -> cId = connectionId)
```

This query will only send back messages from other clients. Messages that the client sent itself will be filtered.

```fs
let query (connectionId: ConnectionId) (msgs: IAsyncObservable<Msg*ConnectionId>) : IAsyncObservable<Msg*ConnectionId> =
    msgs
    |> AsyncRx.filter (fun (msg, cId) -> cId <> connectionId)
```

### QueryAll

This query is of type `IAsyncObservable<'msg*ConnectionId> -> IAsyncObservable<'msg*ConnectionId>` and is a query for the single stream of all messages to and from all clients. This query is processed first, and enables us to send messages that needs to be received by all clients. Thus we may produce messages out of thin air (timers etc) and send them to all connected clients.

### Encode

Encoder for serializing a message to JSON string. The type of encoder is `'msg -> string`.

### Decode

Decoder for deserializing a JSON string to a message. The type of decoder is `string -> 'msg option`.

### RequestPath

Request path where the server will liste for websocket requests. Default is `/ws`. You will usually not need to change this.

