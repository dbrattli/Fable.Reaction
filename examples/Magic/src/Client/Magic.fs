module Magic

open Fable.Core.JsInterop
open Fable.React

open Shared

open Feliz
open Feliz.Bulma

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
            Html.span [
                prop.key (string i)
                prop.style [ style.top (int pos.Y)
                             style.left (offsetX pos.X i)
                             style.position.fixedRelativeToWindow ]
                prop.text pos.Letter
            ]
    ] |> ofList
]

let viewLetters model =
    Html.div [
        prop.style [ style.fontFamily "Consolas, monospace"; style.fontWeight 700; style.height 100 ]
        prop.children [
            match model.Letters with
            | None ->
                Html.none
            | Local letters ->
                yield!  drawLetters letters
            | Remote letters ->
                yield! drawLetters letters
        ]
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
    Bulma.table [
        table.isHoverable
        table.isStriped

        prop.children [
            Html.thead [
                Html.tr [
                    Html.th "Feature"
                    Html.th "Active"
                ]
            ]

            Html.tbody [
                Html.tr [
                    Html.td "Letters"
                    Html.td [
                        Bulma.input.checkbox [
                            prop.isChecked (letterSubscription model)
                            prop.id "letters"
                            prop.onChange (fun (_: bool) -> dispatch ToggleLetters)
                        ]
                    ]
                ]

                Html.tr [
                    Html.td "Letters (string and position) over Websockets"
                    Html.td [
                        Bulma.input.checkbox [
                            prop.isChecked (letterSubscriptionOverWebsockets model)
                            prop.id "remoteLetters"
                            prop.onChange (fun (_: bool) -> dispatch ToggleRemoteLetters)
                        ]
                    ]
                ]
            ]
        ]
    ]

let viewLetterString letterString dispatch =
    match letterString with
    | Show letterString ->
        Html.div [
            Html.text letterString
            Html.text " "

            Bulma.button.a [
                color.isPrimary
                prop.onClick (fun _ -> dispatch EditLetterStringRequested)
                prop.text "Edit"
            ]
        ]

    | Edit letterString ->
        Html.div [
            Bulma.field.div [
                field.isGrouped
                prop.children [
                    Bulma.control.div [
                        Bulma.input.text [
                            prop.placeholder "Magic String"
                            prop.defaultValue letterString
                            prop.onChange (fun (event: Browser.Types.Event) ->  !!event.target?value |> LetterStringEdited |> dispatch)
                        ]
                    ]

                    Bulma.control.div [
                        Bulma.button.a [
                            color.isPrimary
                            prop.onClick (fun _ -> dispatch <| EditLetterStringDone letterString)
                            prop.text "Submit"
                        ]
                    ]
                ]
            ]
        ]


let view model dispatch =
    Html.div [
        prop.style [ style.border(1, borderStyle.dashed, color.black); style.margin 20; style.padding 20 ]
        prop.children [

            Bulma.title.h3 "Magic Component"
            Bulma.subtitle.h4 "Magic String over websockets (when activated)"
            Bulma.columns [
                Bulma.column [
                    viewLetterString model.LetterString dispatch
                ]

                Bulma.column []
            ]

            Bulma.columns [
                Bulma.column [
                    viewStatus dispatch model
                ]
            ]

            Bulma.columns [
                Bulma.column [
                    viewLetters model
                ]
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
