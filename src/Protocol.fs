// From: https://github.com/ncthbrt/Fable.Websockets/blob/master/src/Shared/Protocol.fs

namespace Fable.Reaction.WebSocket

module Protocol =
    open System

    (* RFC6455 compliant mapping of closed codes *)
    type ClosedCode =
        | Normal                    (* 1000 indicates a normal closure, meaning that the purpose for
                                       which the connection was established has been fulfilled.
                                     *)
        | GoingAway                 (* 1001 indicates that an endpoint is "going away", such as a server
                                       going down or a browser having navigated away from a page.
                                     *)
        | ProtocolError             (* 1002 indicates that an endpoint is terminating the connection due
                                       to a protocol error.
                                     *)
        | Unsupported               (* 1003 indicates that an endpoint is terminating the connection
                                       because it has received a type of data it cannot accept (e.g., an
                                       endpoint that understands only text data MAY send this if it
                                       receives a binary message).
                                     *)
        | Reserved of uint16        (* 1004, 1014, 1016-1999
                                       Status codes in this range are reserved for definition by
                                       this protocol, its future revisions, and extensions specified in a
                                       permanent and readily available public specification.
                                     *)
        | NoStatus                  (* 1005 is a reserved value and MUST NOT be set as a status code in a
                                       Close control frame by an endpoint.  It is designated for use in
                                       applications expecting a status code to indicate that no status
                                       code was actually present.
                                     *)
        | Abnormal                  (* 1006 is a reserved value and MUST NOT be set as a status code in a
                                       Close control frame by an endpoint.  It is designated for use in
                                       applications expecting a status code to indicate that the
                                       connection was closed abnormally, e.g., without sending or
                                       receiving a Close control frame.
                                     *)
        | InconsistentData           (* 1007 indicates that an endpoint is terminating the connection
                                       because it has received data within a message that was not
                                       consistent with the type of the message (e.g., non-UTF-8 [RFC3629]
                                       data within a text message).
                                     *)
        | PolicyViolation           (* 1008 indicates that an endpoint is terminating the connection
                                       because it has received a message that violates its policy.  This
                                       is a generic status code that can be returned when there is no
                                       other more suitable status code (e.g., 1003 or 1009) or if there
                                       is a need to hide specific details about the policy.
                                     *)
        | TooLarge                  (* 1009 indicates that an endpoint is terminating the connection
                                       because it has received a message that is too big for it to
                                       process.
                                     *)
        | MissingExtension          (* 1010 indicates that an endpoint (client) is terminating the
                                       connection because it has expected the server to negotiate one or
                                       more extension, but the server didn't return them in the response
                                       message of the WebSocket handshake.  The list of extensions that
                                       are needed SHOULD appear in the /reason/ part of the Close frame.
                                       Note that this status code is not used by the server, because it
                                       can fail the WebSocket handshake instead.
                                     *)
        | InternalError             (* 1011 indicates that a server is terminating the connection because
                                       it encountered an unexpected condition that prevented it from
                                       fulfilling the request.
                                     *)
        | Restarting                (* 1012 The server is terminating the connection because it is
                                       restarting.
                                     *)
        | TryAgainLater             (* 1013 The server is terminating the connection due to a temporary
                                       condition, e.g. it is overloaded and is casting off some of its
                                       clients.
                                     *)
        | TLSHandshake              (* 1015 is a reserved value and MUST NOT be set as a status code in a
                                       Close control frame by an endpoint.  It is designated for use in
                                       applications expecting a status code to indicate that the
                                       connection was closed due to a failure to perform a TLS handshake
                                       (e.g., the server certificate can't be verified).
                                     *)
        | Extension of uint16       (* 2000–2999 is reserved for use by Websocket extensions *)
        | Registered of uint16      (* Status codes in the range 3000-3999 are reserved for use by
                                       libraries, frameworks, and applications.  These status codes are
                                       registered directly with IANA.  The interpretation of these codes
                                       is undefined by this protocol.
                                     *)
        | Application of uint16     (* Status codes in the range 4000-4999 are reserved for private
                                       application use and thus can't be registered.  Such codes can be
                                       used by prior agreements between WebSocket applications.
                                       The interpretation of these codes is undefined by this protocol.
                                     *)
        | OutOfRange of uint16      (* Codes in the range 0-999 are not used in the websocket
                                       specification. While those >=5000 are left undefined and hence
                                       should not be used
                                     *)
    let public toClosedCode (code: uint16) =
      match code with
      | 1000us -> Normal
      | 1001us -> GoingAway
      | 1002us -> ProtocolError
      | 1003us -> Unsupported
      | 1005us -> NoStatus
      | 1006us -> Abnormal
      | 1007us -> InconsistentData
      | 1008us -> PolicyViolation
      | 1009us -> TooLarge
      | 1010us -> MissingExtension
      | 1011us -> InternalError
      | 1012us -> Restarting
      | 1013us -> TryAgainLater
      | 1015us -> TLSHandshake
      | code when (code <= 1999us && code>=1000us) -> Reserved code
      | code when (code>=2000us && code<=2999us) -> Extension code
      | code when (code>=3000us && code<=3999us) -> Registered code
      | code when (code>=4000us && code<=4999us) -> Application code
      | code -> OutOfRange code

    let public fromClosedCode (code: ClosedCode) =
      match code with
      | Normal -> 1000us
      | GoingAway -> 1001us
      | ProtocolError -> 1002us
      | Unsupported -> 1003us
      | NoStatus -> 1005us
      | Abnormal -> 1006us
      | InconsistentData -> 1007us
      | PolicyViolation -> 1008us
      | TooLarge -> 1009us
      | MissingExtension -> 1010us
      | InternalError -> 1011us
      | Restarting -> 1012us
      | TLSHandshake -> 1015us
      | Reserved r -> r
      | Extension e -> e
      | Registered r -> r
      | Application a -> a
      | OutOfRange o -> failwithf "Status code %d not in valid range. Rather use 4000-4999" o

    type ClosedEvent = { code: ClosedCode; reason:string; wasClean: bool }

    type WebsocketEvent<'applicationProtocol> =
        | Msg of 'applicationProtocol
        | Closed of ClosedEvent
        | Opened
        | Error
        | Exception of Exception

    type CloseHandle = ClosedCode -> string -> unit

    type SendMessage<'protocol> = 'protocol -> unit

    type ReadyState =
      | Connecting
      | Open
      | Closing
      | Closed

    let toReadyState: uint16 -> ReadyState =
       function
       | 0us -> Connecting
       | 1us -> Open
       | 2us -> Closing
       | 3us -> Closed
       | code -> failwithf "Invalid ready state %d" code

    let fromReadyState: ReadyState -> uint16 =
       function
       | Connecting -> 0us
       | Open -> 1us
       | Closing -> 2us
       | Closed -> 3us