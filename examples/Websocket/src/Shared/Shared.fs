namespace Shared

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type Counter = int

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    | Increment
    | Decrement
    | InitialCountLoaded of Result<Counter, exn>

    static member Encode (msg: Msg) : string =
        let data =
            match msg with
            | Increment ->
                Encode.object [ "msg", Encode.string "increment"]
            | Decrement ->
                Encode.object [ "msg", Encode.string "decrement"]
            | _ ->
                Encode.object []

        Encode.toString 4 data

    static member Decode (json: string) : Option<Msg> =
        let decodeMsg =
            (Decode.field "msg" Decode.string)
            |> Decode.map (fun str ->
                            match str with
                            | "increment" -> Increment
                            | "decrement" -> Decrement
                            | _ -> Decrement
                          )
        let result = Decode.fromString decodeMsg json

        match result with
        | Ok msg ->
            Some msg
        | Error err ->
            None
