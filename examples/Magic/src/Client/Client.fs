module Client

open Fable.React
open Fable.React.Props
open Fetch.Types

open Fulma
open Fable.Reaction
open FSharp.Control

open Thoth.Json

type AppModel = {
    InitialString: string
    Info : Info.Model
}

type Model =
    | Loading
    | Error of string
    | App of AppModel


type Msg =
    | InitialLetterStringLoaded of Result<string, exn>
    | MagicMsg of Magic.Msg
    | InfoMsg of Info.Msg

let initialModel =
    Loading

let update model (msg : Msg) =
    match model, msg with
    | Loading, InitialLetterStringLoaded (Ok letterString) ->
        App {
            InitialString = letterString
            Info = Info.init letterString
        }
    | Loading, InitialLetterStringLoaded (Result.Error exn) ->
        Error exn.Message
    | _ -> model

let safeComponents =
    let components =
        span [] [
            a [ Href "https://github.com/giraffe-fsharp/Giraffe" ] [ str "Giraffe" ]
            str ", "
            a [ Href "http://fable.io" ] [ str "Fable" ]
            str ", "
            a [ Href "https://mangelmaxime.github.io/Fulma" ] [ str "Fulma" ]
            str ", "
            a [ Href "https://github.com/dbrattli/Fable.Reaction" ] [ str "Reaction" ]
        ]

    p [] [
        strong [] [ str "SAFE Template" ]
        str " powered by: "
        components
    ]


let viewApp model dispatch =
    match model with
    | Loading ->
        div [] [ str "Initial Values not loaded" ]

    | Error error ->
        div [] [ str <| "Something went wrong: " + error ]

    | App model ->
        div [] [
            Magic.magic model.InitialString ()
            Info.info model.InitialString ()
        ]

let view (model : Model) (dispatch : Msg -> unit) =
    div [] [
        Navbar.navbar [ Navbar.Color IsPrimary ] [
            Navbar.Item.div [] [
                Heading.h2 [] [
                    str "Fable Reaction Playground"
                ]
            ]
        ]

        Container.container []  [ viewApp model dispatch ]

        Footer.footer [] [
            Content.content [ Content.Modifiers [
                                Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                            ] ] [
                safeComponents
            ]
        ]
    ]

// Fetch a data structure from specified url and using the decoder
let fetchWithDecoder<'T> (url: string) (decoder: Decoder<'T>) (init: RequestProperties list) =
    promise {
        let! response = GlobalFetch.fetch(RequestInfo.Url url, Fetch.requestProps init)
        let! body = response.text()
        return Decode.unsafeFromString decoder body
    }

// Inline the function so Fable can resolve the generic parameter at compile time
let inline fetchAs<'T> (url: string) (init: RequestProperties list) =
    // In this example we use Thoth.Json cached auto decoders
    // More info at: https://mangelmaxime.github.io/Thoth/json/v3.html#caching
    let decoder = Decode.Auto.generateDecoderCached<'T>()
    fetchWithDecoder url decoder init

let stream model msgs =
    match model with
    | Loading ->
        AsyncRx.ofPromise (fetchAs<string> "/api/init" [])
        |> AsyncRx.map (Ok >> InitialLetterStringLoaded)
        |> AsyncRx.catch (Result.Error >> InitialLetterStringLoaded >> AsyncRx.single)
        |> AsyncRx.tag "loading"

    | Error exn -> msgs |> AsyncRx.tag "error"
    | App model -> msgs |> AsyncRx.tag "msgs"

let app = Reaction.StreamView initialModel view update stream
mountById "reaction-app" (ofFunction app () [])
