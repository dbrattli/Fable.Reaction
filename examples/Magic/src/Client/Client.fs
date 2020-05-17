module Client

open Fetch.Types

open Fable.React
open Fable.Reaction
open FSharp.Control
open Feliz
open Feliz.Bulma

open Thoth.Json
open System

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
        Html.span [
            Html.a [ prop.href "https://github.com/giraffe-fsharp/Giraffe"; prop.text "Giraffe" ]
            Html.text ", "
            Html.a [ prop.href "http://fable.io"; prop.text "Fable" ]
            Html.text ", "
            Html.a [ prop.href "https://mangelmaxime.github.io/Fulma"; prop.text "Fulma" ]
            Html.text ", "
            Html.a [ prop.href "https://github.com/dbrattli/Fable.Reaction"; prop.text "Reaction" ]
        ]

    Html.p [
        Html.strong "SAFE Template"
        Html.text " powered by: "
        components
    ]


let viewApp model dispatch =
    match model with
    | Loading ->
        Html.div "Initial Values not loaded"

    | Error error ->
        Html.div ("Something went wrong: " + error)

    | App model ->
        Html.div [
            Magic.magic model.InitialString
            Info.info model.InitialString
        ]

let view (model : Model) (dispatch : Msg -> unit) =
    Html.div [
        Bulma.navbar [
            color.isPrimary
            prop.children [
                Bulma.navbarItem.div [
                    Bulma.title.h2 "Fable Reaction Playground"
                ]
            ]
        ]

        Bulma.container [ viewApp model dispatch ]

        Bulma.footer [
            Bulma.content [
                prop.style [ style.textAlign.center ]
                prop.children [
                    safeComponents
                ]
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

let app props = Reaction.StreamView initialModel view update stream
mountById "reaction-app" (ofFunction app () [])
