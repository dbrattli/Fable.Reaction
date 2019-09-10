=========
WebSocket
=========

Fable.Reaction enables you to route stream of messages to the server and
back again using "message channels".

Note that server side support for WebSocket message handling must also
be in place using ``Elmish.Streams.AspNetCoreMiddleWare``.

Message Channel
===============

A message channel is an ``AsyncRx`` operator that composes a WebSocket stream
of messages into the reactive query. This basically routes the messages to the
server and (possibly) back again.

A message channnel works like this:

- Every message that enters the operator will be sent to the server over
  a WebSocket.

- Every message received from the server will be pushed to observers or
  the next composed operator.

This enables us to compose the message channel into the qury without
resorting to imperative programming.

.. val:: msgChannel<'msg>
    :type: uri:string -> encode:('msg -> string) -> decode:(string -> 'msg option) -> source:IAsyncObservable<'msg> -> IAsyncObservable<'msg>

    **Example**

    .. code:: fsharp

        let stream (msgs: Stream<Msgs, string>) =
            msgs
            |> AsyncRx.msgChannel "ws://localhost:8085/ws" Msg.Encode Msg.Decode
            |> AsyncRx.toStream "msgs"

