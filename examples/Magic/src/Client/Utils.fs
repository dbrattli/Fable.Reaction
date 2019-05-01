module Utils

open Elmish.Streams

let server source =
  AsyncRx.msgChannel<Shared.Msg>
    "ws://localhost:8085/ws"
    Shared.Msg.Encode
    Shared.Msg.Decode
    source