module Client

open Elmish
open Elmish.React

open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Thoth.Json

open Shared

open Fulma
open Fulma.Extensions

open Fable.Reaction
open Fable.Reaction.WebSocket
open Reaction
open Shared



// module SideApp =

//   type Model =
//     {
//       LetterString : string
//       Msgs : int
//       Remote : bool
//     }

//   type Msg =
//     | RemoteToggled


//   let init : Model =
//     {
//       LetterString = ""
//       Msgs = 0
//       Remote = false
//     }



type Model =
  {
    Magic : Magic.Model
  }

type Msg =
  | MagicMsg of Magic.Msg

let init () =
  {
    Magic = Magic.init();
  }

let update (msg : Msg) model =
  match msg with
  | MagicMsg msg ->
      { model with Magic = Magic.update msg model.Magic }



let safeComponents =
  let components =
    span [ ]
       [
         a [ Href "https://github.com/giraffe-fsharp/Giraffe" ] [ str "Giraffe" ]
         str ", "
         a [ Href "http://fable.io" ] [ str "Fable" ]
         str ", "
         a [ Href "https://elmish.github.io/elmish/" ] [ str "Elmish" ]
         str ", "
         a [ Href "https://mangelmaxime.github.io/Fulma" ] [ str "Fulma" ]
       ]

  p [ ]
    [ strong [] [ str "SAFE Template" ]
      str " powered by: "
      components ]



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

      Magic.view model.Magic (MagicMsg >> dispatch)

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


let query (model: Model) (msgs: IAsyncObservable<Msg>) =
  let magicMsgs =
    Program.withSubQuery Magic.query model.Magic msgs MagicMsg asMagicMsg

  magicMsgs


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkSimple init update view
|> Program.withQuery query
#if DEBUG
// |> Program.withConsoleTrace
// |> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
