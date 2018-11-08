module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props

open Fulma
open Reaction.AsyncRx
open Elmish.Reaction

type Model =
  {
    Magic : Magic.Model
    Info : Info.Model
  }

type Msg =
  | MagicMsg of Magic.Msg
  | InfoMsg of Info.Msg

let init () =
  {
    Magic = Magic.init
    Info = Info.init
  }

let update (msg : Msg) model =
  match msg with
  | MagicMsg msg ->
      { model with Magic = Magic.update msg model.Magic }

  | InfoMsg msg ->
      { model with Info = Info.update msg model.Info }



let safeComponents =
  let components =
    span []
     [
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


let view (model : Model) (dispatch : Msg -> unit) =
  div []
    [
      Navbar.navbar [ Navbar.Color IsPrimary ]
        [
          Navbar.Item.div []
            [ Heading.h2 []
                [ str "SAFE Template with Fable.Reaction" ]
            ]
        ]

      Container.container []
        [
          Magic.view model.Magic (MagicMsg >> dispatch)
          Info.view model.Info (InfoMsg >> dispatch)
        ]


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

let toMagicMsgs msgs =
    msgs |> AsyncRx.choose asMagicMsg

let asInfoMsg msg =
  match msg with
  | InfoMsg msg ->
      Some msg
  | _ ->
      None

let toInfoMsgs msgs =
    msgs |> AsyncRx.choose asInfoMsg

let query (model: Model) (msgs: IAsyncObservable<Msg>) =
    Queries [
        Subscribe (msgs, "msgs")

        Magic.query model.Magic (toMagicMsgs msgs) |> Query.map MagicMsg
        Info.query model.Info (toInfoMsgs msgs) |> Query.map InfoMsg
    ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkSimple init update view
|> Program.withQuery query
#if DEBUG
//|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
