module Utils

open Fable.Reaction

let server source =
  AsyncRx.msgChannel<Shared.Msg>
    "ws://localhost:8085/ws"
    Shared.Msg.Encode
    Shared.Msg.Decode
    source