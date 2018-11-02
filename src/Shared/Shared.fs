namespace Shared

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type Counter = int

type LetterPos = {
    Letter: string
    X: float
    Y: float
}

type Msg =
| Letter of int * LetterPos

type Msg with
    static member Encode (msg: Msg) : string =
        Encode.Auto.toString(4, msg)

    static member Decode (json: string) : Msg option =
        let result = Decode.Auto.fromString<Msg>(json)
        match result with
        | Ok msg -> Some msg
        | Error _ -> None