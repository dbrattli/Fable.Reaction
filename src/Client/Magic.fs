module Magic

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

type LetterSource =
| None
| Local of Map<int,LetterPos>
| Remote of Map<int,LetterPos>

type AppModel =
    {
      Counter: Counter
      Letters : LetterSource
      LetterString : string
    }

type Model =
| Loading
| App of AppModel



type Msg =
| ToggleLetters
| ToggleRemoteLetters
| LetterStringChanged of string
| InitialCountLoaded of Result<Counter, exn>
| Letter of int * LetterPos
| RemoteMsg of Shared.Msg

let init () =
  Loading

let loadCountCmd () =
    ofPromise (fetchAs<int> "/api/init" Decode.int [])
    |> AsyncObservable.map (Ok >> InitialCountLoaded)
    |> AsyncObservable.catch (Error >> InitialCountLoaded >> AsyncObservable.single)


let withToggledLetters model =
  match model.Letters with
  | None ->
      { model with Letters = Local Map.empty }

  | Remote letters ->
      { model with Letters = Local letters }

  | Local _ ->
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
  match model, msg with
  | App appModel, LetterStringChanged str ->
      { appModel with LetterString = str}
      |> App

  | App appModel, ToggleLetters ->
      appModel
      |> withToggledLetters
      |> App

  | App appModel, ToggleRemoteLetters ->
      appModel
      |> withToggledRemoterLetters
      |> App

  | Loading, InitialCountLoaded (Ok initialCount)->
      { Counter = initialCount ; Letters = None ; LetterString = "Magic Released!" }
      |> App

  | App appModel, RemoteMsg (Shared.Letter (index, pos)) ->
      match appModel.Letters with
      | Remote letters ->
          App { appModel with Letters = Remote <| letters.Add (index, pos) }

      | _ -> App appModel


  | App appModel, Letter (index, pos) ->
      match appModel.Letters with
      | Local letters ->
          App { appModel with Letters = Local <| letters.Add (index, pos) }

      | _ -> App appModel


  | _ -> model


let show = function
| App { Counter = x } -> string x
| Loading -> "Loading..."


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


let letterSubscription appModel =
  match appModel.Letters with
  | Local _ ->
      true

  | _ ->
      false

let letterSubscriptionOverWebsockets appModel =
  match appModel.Letters with
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


let viewApp model dispatch =
  Container.container []
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

let view model dispatch =
  match model with
  | Loading ->
      div [] [ str "Initial Values not loaded" ]

  | App appModel ->
      viewApp appModel dispatch



let server source =
  msgChannel<Shared.Msg>
    "ws://localhost:8085/ws"
    Shared.Msg.Encode
    Shared.Msg.Decode
    source

let letterStream letterString =
  letterString
  |> Seq.toList // Split into list of characters
  |> Seq.mapi (fun i c -> i, c) // Create a tuple with the index
  |> AsyncObservable.ofSeq // Make this an observable
  |> AsyncObservable.flatMap (fun (i, letter) ->
      ofMouseMove ()
      |> AsyncObservable.delay (100 * i)
      |> AsyncObservable.map (fun ev -> (i, { Letter = string letter; X = ev.clientX; Y = ev.clientY }))
  )

let query (model : Model) msgs =
    match model with
    | App appModel ->
        match appModel.Letters with
        | Local letters ->
            appModel.LetterString
            |> letterStream
            |> AsyncObservable.map Letter
            |> AsyncObservable.merge msgs
            , (appModel.LetterString + "_local")

         | Remote _ ->
            appModel.LetterString
            |> letterStream
            |> AsyncObservable.map Shared.Msg.Letter
            |> server
            |> AsyncObservable.map RemoteMsg
            |> AsyncObservable.merge msgs
            , (appModel.LetterString + "_remote")

        | _ ->
              msgs,"none"

    | Loading ->
        loadCountCmd (),"loading"



