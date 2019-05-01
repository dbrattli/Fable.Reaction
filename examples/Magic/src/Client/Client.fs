module Client

open Elmish
open Elmish.React

open Fable.React
open Fable.React.Props
open Fetch.Types

open Fulma
open Elmish.Streams
open FSharp.Control

open Thoth.Json

type AppModel =
  {
    Magic : Magic.Model
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

let init () =
  Loading

let update (msg : Msg) model =
  match model, msg with
  | Loading, InitialLetterStringLoaded (Ok letterString) ->
      App <|
        {
          Magic = Magic.init letterString
          Info = Info.init letterString
        }

  | Loading, InitialLetterStringLoaded (Result.Error exn) ->
      Error exn.Message

  | App model, MagicMsg msg ->
      { model with Magic = Magic.update msg model.Magic }
      |> App

  | App model, InfoMsg msg ->
      { model with Info = Info.update msg model.Info }
      |> App

  | _ -> model

let safeComponents =
  let components =
    span []
     [
       a [ Href "https://github.com/dbrattli/Reaction" ] [ str "Reaction" ]
       str ", "
       a [ Href "https://github.com/giraffe-fsharp/Giraffe" ] [ str "Giraffe" ]
       str ", "
       a [ Href "http://fable.io" ] [ str "Fable" ]
       str ", "
       a [ Href "https://elmish.github.io/elmish/" ] [ str "Elmish" ]
       str ", "
       a [ Href "https://mangelmaxime.github.io/Fulma" ] [ str "Fulma" ]
     ]

  p []
    [
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
     div []
      [
        Magic.view model.Magic (MagicMsg >> dispatch)
        Info.view model.Info (InfoMsg >> dispatch)
      ]

let view (model : Model) (dispatch : Msg -> unit) =
    div [] [
        Navbar.navbar [ Navbar.Color IsPrimary ] [
            Navbar.Item.div [] [
                Heading.h2 [] [
                    str "Elmish.Streams Playground"
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

let asMagicMsg msg =
    match msg with
    | MagicMsg msg ->
        Some msg
    | _ ->
        None

let asInfoMsg msg =
    match msg with
    | InfoMsg msg ->
        Some msg
    | _ ->
        None

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
        |> AsyncRx.toStream "loading"

    | Error exn ->
        msgs

    | App model ->
        msgs
        |> Stream.subStream Magic.stream model.Magic MagicMsg asMagicMsg "magic"
        |> Stream.subStream Info.stream model.Info InfoMsg asInfoMsg "info"

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkSimple init update view
|> Program.withStream stream "msgs"
#if DEBUG
//|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
//|> Program.withDebugger
#endif
|> Program.run
