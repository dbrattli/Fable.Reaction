module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props

open Fulma
open Reaction
open Fable.PowerPack.Fetch

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
  div []
    [
      Navbar.navbar [ Navbar.Color IsPrimary ]
        [
          Navbar.Item.div []
            [
              Heading.h2 []
                [ str "Fable.Reaction Playground" ]
            ]
        ]

      Container.container []  [ viewApp model dispatch ]

      Footer.footer []
        [
          Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
            [ safeComponents ]
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

let loadLetterString () =
  AsyncRx.ofPromise (fetchAs<string> "/api/init" Decode.string [])
  |> AsyncRx.map (Ok >> InitialLetterStringLoaded)
  |> AsyncRx.catch (Result.Error >> InitialLetterStringLoaded >> AsyncRx.single)
  |> AsyncRx.asStream "loading"


let stream model msgs =
  match model with
  | Loading ->
      loadLetterString ()

  | Error exn ->
      msgs

  | App model ->
      let magicMsgs =
        msgs
        |> Stream.choose asMagicMsg
        |> Magic.stream model.Magic
        |> Stream.map MagicMsg
      let infoMsgs =
        msgs
        |> Stream.choose asInfoMsg
        |> Info.stream model.Info
        |> Stream.map InfoMsg

      Streams
        [
          msgs
          magicMsgs
          infoMsgs
        ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkSimple init update view
|> Program.withMsgStream stream "msgs"
#if DEBUG
//|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
