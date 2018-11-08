module Magic

open Elmish

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
open Utils

type LetterSource =
| None
| Local of Map<int,LetterPos>
| Remote of Map<int,LetterPos>

type Model =
  {
    Letters : LetterSource
    LetterString : string
  }

type Msg =
  | ToggleLetters
  | ToggleRemoteLetters
  | LetterStringChanged of string
  | Letter of int * LetterPos
  | RemoteMsg of Shared.Msg

let init letterString =
  {
    Letters = None
    LetterString = letterString
  }

let withToggledLetters model =
  match model.Letters with
  | None ->
      printfn "None"
      { model with Letters = Local Map.empty }

  | Remote letters ->
      { model with Letters = Local letters }

  | Local _ ->
      printfn "Letters=None"
      { model with Letters = None }

let withToggledRemoterLetters model =
  match model.Letters with
  | None ->
      { model with Letters = Remote Map.empty }

  | Remote _ ->
      { model with Letters = None }

  | Local letters ->
      { model with Letters = Remote letters }

let update (msg : Msg) (model : Model) : Model =
  match msg with
  | LetterStringChanged str ->
      { model with LetterString = str}

  | ToggleLetters ->
      model |> withToggledLetters

  | ToggleRemoteLetters ->
      model |> withToggledRemoterLetters

  | RemoteMsg (Shared.Letter (index, pos)) ->
      match model.Letters with
      | Remote letters ->
          { model with Letters = Remote <| letters.Add (index, pos) }

      | _ -> model


  | Letter (index, pos) ->
      match model.Letters with
      | Local letters ->
          { model with Letters = Local <| letters.Add (index, pos) }

      | _ -> model


  | _ -> model


let offsetX x i =
  (int x) + i * 10 + 15

let drawLetters letters =
  [
    for KeyValue(i, pos) in letters do
      yield span [ Style [Top pos.Y; Left (offsetX pos.X i); Position "absolute"] ]
          [ str pos.Letter ]
  ]

let viewLetters model =
  match model.Letters with
  | None ->
        str ""

  | Local letters ->
      letters |> drawLetters |> div []

  | Remote letters ->
      letters |> drawLetters |> div []

  |> List.singleton
  |> div [ Style [ FontFamily "Consolas, monospace"; FontWeight "Bold"; Height "100%"] ]


let letterSubscription model =
  match model.Letters with
  | Local _ ->
      true

  | _ ->
      false

let letterSubscriptionOverWebsockets model =
  match model.Letters with
  | Remote _ ->
      true

  | _ ->
      false

let viewStatus dispatch model =
  Table.table [ Table.IsHoverable ; Table.IsStriped ]
    [
      thead []
        [
          tr []
            [
              th [] [ str "Feature" ]
              th [] [ str "Active" ]
            ]
        ]

      tbody [ ]
        [
          tr []
           [
             td [] [ str "Letters" ]
             td []
              [
                Switch.switch
                  [
                    Switch.Checked <| letterSubscription model
                    Switch.OnChange (fun _ -> dispatch ToggleLetters)
                  ] []
              ]
           ]

          tr []
           [
             td [] [ str "Letters over Websockets" ]
             td []
              [
                Switch.switch
                  [
                    Switch.Checked <| letterSubscriptionOverWebsockets model
                    Switch.OnChange (fun _ -> dispatch ToggleRemoteLetters)
                  ] []
              ]
           ]
        ]
    ]


let view model dispatch =
  div []
    [
      Columns.columns []
        [
          Column.column []
            [
              form []
                [
                  Field.div []
                    [
                      Label.label [] [ str "Magic String" ]
                      Control.div []
                        [
                          Input.text
                            [
                              Input.Placeholder "Magic String"
                              Input.DefaultValue model.LetterString
                              Input.Props [ OnChange (fun event -> LetterStringChanged (!!event.target?value) |> dispatch) ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

      Columns.columns []
          [ Column.column [] [ viewStatus dispatch model ] ]

      Columns.columns []
          [ Column.column [] [ viewLetters model ] ]
    ]

let letterStream letterString =
  letterString
  |> Seq.toList // Split into list of characters
  |> Seq.mapi (fun i c -> i, c) // Create a tuple with the index
  |> AsyncRx.ofSeq // Make this an observable
  |> AsyncRx.flatMap (fun (i, letter) ->
      ofMouseMove ()
      |> AsyncRx.delay (100 * i)
      |> AsyncRx.map (fun ev -> (i, { Letter = string letter; X = ev.clientX; Y = ev.clientY }))
  )


let query (model : Model) msgs =
  match model.Letters with
  | Local _ ->
      let xs =
        model.LetterString
        |> letterStream
        |> AsyncRx.map Letter

      Subscribe (xs, model.LetterString + "_local")

   | Remote _ ->
      let letterQuery =
        model.LetterString
          |> letterStream
          |> AsyncRx.map Shared.Msg.Letter

      let letterStringQuery =
        msgs
        |> AsyncRx.choose (function | LetterStringChanged str -> Some str | _ -> Option.None)
        |> AsyncRx.map Shared.Msg.LetterString

      let xs =
        letterStringQuery
        |> AsyncRx.merge letterQuery
        |> server
        |> AsyncRx.map RemoteMsg

      Subscribe (xs, model.LetterString + "_remote")

  | _ ->
        Dispose
