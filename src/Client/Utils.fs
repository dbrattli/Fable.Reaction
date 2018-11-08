module Utils

open Elmish.Reaction.WebSocket

let server source =
  msgChannel<Shared.Msg>
    "ws://localhost:8085/ws"
    Shared.Msg.Encode
    Shared.Msg.Decode
    source