module Magic

open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props

open Shared

open Fulma
open Fulma.Extensions.Wikiki

open FSharp.Control
open Fable.Reaction
open Utils

type LetterSource =
    | None
    | Local of Map<int,LetterPos>
    | Remote of Map<int,LetterPos>

type LetterString =
    | Show of string
    | Edit of string

type Model = {
    Letters : LetterSource
    LetterString : LetterString
}

type Msg =
    | ToggleLetters
    | ToggleRemoteLetters
    | EditLetterStringRequested
    | EditLetterStringDone of string
    | LetterStringEdited of string
    | Letter of int * LetterPos
    | RemoteMsg of Shared.Msg

let init letterString = {
    Letters = None
    LetterString = Show letterString
}

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

let withLetterString letterString model =
    match model.LetterString with
    | Show _ ->
        { model with LetterString = Show letterString }

    | Edit _ ->
        { model with LetterString = Edit letterString }

let withEditLetterString model =
    match model.LetterString with
    | Show letterString ->
        { model with LetterString = Edit letterString }

    | Edit _ ->
        { model with
            Letters = None
        }

let withEditedLetterString letterString model =
    match model.LetterString with
    | Show letterString ->
        model

    | Edit _ ->
        { model with LetterString = Edit letterString }

let withShowLetterString letterString model =
    { model with LetterString = Show letterString }

let update (model : Model) (msg : Msg) : Model =
    match msg with
    | EditLetterStringRequested ->
        model |> withEditLetterString

    | EditLetterStringDone letterString ->
        model |> withShowLetterString letterString

    | LetterStringEdited letterString ->
        model |> withEditedLetterString letterString

    | ToggleLetters ->
        model |> withToggledLetters

    | ToggleRemoteLetters ->
        model |> withToggledRemoterLetters

    | RemoteMsg (Shared.LetterStringChanged letterString) ->
        model |> withLetterString letterString

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


let offsetX x i =
    (int x) + i * 10 + 15

let drawLetters letters = [
    [
        for KeyValue(i, pos) in letters do
            yield span [ Key (string i)
                         Style [Top pos.Y
                                Left (offsetX pos.X i)
                                Position PositionOptions.Fixed ]] [
                    str pos.Letter
                ]
    ] |> ofList
]

let viewLetters model =
    div [ Style [ FontFamily "Consolas, monospace"; FontWeight "Bold"; Height "100%"] ] [
        match model.Letters with
        | None ->
            yield str ""
        | Local letters ->
            yield!  drawLetters letters
        | Remote letters ->
            yield! drawLetters letters
    ]


let letterSubscription model =
    match model.Letters with
    | Local _ -> true
    | _ -> false

let letterSubscriptionOverWebsockets model =
  match model.Letters with
  | Remote _ -> true
  | _ -> false

let viewStatus dispatch model =
    Table.table [ Table.IsHoverable ; Table.IsStriped ] [
        thead [] [
            tr [] [
                th [] [ str "Feature" ]
                th [] [ str "Active" ]
            ]
        ]

        tbody [ ] [
            tr [] [
                td [] [ str "Letters" ]
                td [] [
                    Switch.switch [
                        Switch.Checked <| letterSubscription model
                        Switch.OnChange (fun _ -> dispatch ToggleLetters)
                        Switch.Id "letters"
                    ] []
                ]
            ]

            tr [] [
                td [] [ str "Letters (string and position) over Websockets" ]
                td [] [
                    Switch.switch [
                        Switch.Checked <| letterSubscriptionOverWebsockets model
                        Switch.OnChange (fun _ -> dispatch ToggleRemoteLetters)
                        Switch.Id "remoteLetters"
                    ] []
                ]
            ]
        ]
    ]

let viewLetterString letterString dispatch =
    match letterString with
    | Show letterString ->
        div [] [
            str letterString
            str " "

            Button.button [
                Button.Color IsPrimary
                Button.OnClick (fun _ -> dispatch EditLetterStringRequested)
            ] [ str "Edit" ]
        ]

    | Edit letterString ->
        div [] [
            Field.div [ Field.IsGrouped] [
                Control.div [] [
                    Input.text [
                        Input.Placeholder "Magic String"
                        Input.DefaultValue letterString
                        Input.Props [ OnChange (fun event ->  !!event.target?value |> LetterStringEdited |> dispatch) ]
                    ]
                ]

                Control.div [] [
                  Button.button
                    [
                      Button.Color IsPrimary
                      Button.OnClick (fun _ -> dispatch <| EditLetterStringDone letterString)
                    ] [ str "Submit" ]
                ]
            ]
        ]


let view model dispatch =
    div [ Style [ Border "1px dashed"; Margin "20px"; Padding "20px" ]] [
        Heading.h3 [] [ str "Magic Component" ]
        Heading.h4 [ Heading.IsSubtitle ] [ str "Magic String over websockets (when activated)" ]
        Columns.columns [] [
            Column.column [] [
                viewLetterString model.LetterString dispatch
            ]

            Column.column [] []
        ]

        Columns.columns [] [
            Column.column [] [
                viewStatus dispatch model
            ]
        ]

        Columns.columns [] [
            Column.column [] [
                viewLetters model
            ]
        ]
    ]

// Create timefiles stream of letter string
let letterStream letterString =
    letterString
    |> Seq.toList // Split into list of characters
    |> Seq.mapi (fun i c -> i, c) // Create a tuple with the index
    |> AsyncRx.ofSeq // Make this an observable
    |> AsyncRx.flatMap (fun (i, letter) ->
        AsyncRx.ofMouseMove ()
        |> AsyncRx.delay (100 * i)
        |> AsyncRx.requestAnimationFrame
        |> AsyncRx.map (fun ev -> (i, { Letter = string letter; X = ev.clientX; Y = ev.clientY }))
    )

let extractedLetterString letterString =
    match letterString with
    | Show letterString -> letterString
    | Edit _ -> ""

open System.Threading.Tasks
let stream model msgs =

    match model.Letters with
    | Local _ ->
        let letterString =
            model.LetterString |> extractedLetterString

        letterString
        |> letterStream
        |> AsyncRx.map Letter
        |> AsyncRx.merge msgs
        |> AsyncRx.tag (letterString + "_local")

    | Remote _ ->
        let stringQuery =
            msgs
            |> AsyncRx.choose (function | EditLetterStringDone letterString -> Some letterString | _ -> Option.None)

        let letterStringQuery =
            stringQuery
            |> AsyncRx.map LetterStringChanged

        let remote =
            stringQuery
            |> AsyncRx.startWith [model.LetterString |> extractedLetterString]
            |> AsyncRx.flatMapLatest letterStream
            |> AsyncRx.map Shared.Msg.Letter
            |> AsyncRx.merge letterStringQuery
            |> server
//            |> AsyncRx.retry 10
            |> AsyncRx.map RemoteMsg

        remote
        |> AsyncRx.merge msgs
        |> AsyncRx.tag "_remote"
    | _ ->
        msgs
        |> AsyncRx.tag "msgs"

let magic initialString =
    let initialModel = init initialString

    Reaction.StreamView initialModel view update stream
