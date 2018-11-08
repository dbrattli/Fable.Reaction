module Info

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

open Reaction.AsyncRx
open Elmish.Reaction
open Elmish.Reaction.WebSocket
open Shared


type Model =
  {
    LetterString : string
    Msgs : int
    Remote : bool
  }

type Msg =
  | RemoteToggled
  | MsgAdded
  | LetterStringChanged of string


let init : Model =
  {
    LetterString = ""
    Msgs = 0
    Remote = false
  }

let update msg model =
  match msg with
  | MsgAdded ->
      { model with Msgs = model.Msgs + 1 }

  | RemoteToggled ->
      { model with Remote = not model.Remote }

  | LetterStringChanged str ->
      { model with LetterString = str }


let viewStatus dispatch model =
  Table.table [ Table.IsHoverable ; Table.IsStriped ]
    [
      thead []
        [
          tr []
            [
              th [] [ str "Remote" ]
              th []
                [
                   Switch.switch
                    [
                      Switch.Checked model.Remote
                      Switch.OnChange (fun _ -> dispatch RemoteToggled)
                    ] []
                ]
            ]
        ]

      tbody [ ]
        [
          tr []
           [
             td [] [ str "Number of remote msgs" ]
             td []
              [
                str <| string model.Msgs
              ]
           ]

          tr []
             [
               td [] [ str "Letter string" ]
               td []
                [
                  str model.LetterString
                ]
             ]
        ]
    ]


let view model dispatch =
  Container.container []
    [
      Columns.columns []
        [ Column.column [] [ viewStatus dispatch model ] ]
    ]


let server source =
  msgChannel<Shared.Msg>
    "ws://localhost:8085/ws"
    Shared.Msg.Encode
    Shared.Msg.Decode
    source

let query (model : Model) (msgs : IAsyncObservable<Msg>) =
  if model.Remote then
    let ws =
      msgs
        |> AsyncRx.filter (fun _ -> false)
        |> AsyncRx.map (fun _ -> Shared.Msg.LetterString "dont understand")
        |> server
        |> AsyncRx.share

    let letterStringQuery =
      ws
      |> AsyncRx.choose (function | Shared.Msg.LetterString str -> Some (LetterStringChanged str) | _ -> None)

    let xs =
      ws
        |> AsyncRx.map (fun _ -> MsgAdded)
        |> AsyncRx.merge letterStringQuery

    Subscribe (xs, "remote")
  else
    Dispose