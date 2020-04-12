module Info

open Fable.React

open Fable.Reaction
open FSharp.Control
open Feliz
open Feliz.Bulma

open Utils

type Model = {
    LetterString : string
    Msgs : int
    Remote : bool
}

type Msg =
    | RemoteToggled
    | MsgAdded
    | LetterStringChanged of string


let init letterString = {
    LetterString = letterString
    Msgs = 0
    Remote = false
}

let update model msg =
    match msg with
    | MsgAdded ->
        { model with Msgs = model.Msgs + 1 }

    | RemoteToggled ->
        { model with Remote = not model.Remote }

    | LetterStringChanged str ->
        { model with LetterString = str }

let viewStatus dispatch model =
    Bulma.table [
        table.isHoverable
        table.isStriped
        prop.children [
            Html.thead [
                Html.tr [
                    Html.th [ str "Remote (string and number of messages over websockets)" ]
                    Html.th [
                        Bulma.checkboxLabel [
                            Bulma.checkboxInput [
                                prop.isChecked model.Remote
                                prop.id "remoteInfo"
                                prop.onChange (fun (_: bool) -> dispatch RemoteToggled)
                            ]
                        ]
                    ]
                ]
            ]

            Html.tbody [
                Html.tr [
                    Html.td "Number of remote msgs"
                    Html.td (string model.Msgs)
                ]

                Html.tr [
                    Html.td "Current string (updated when websocket is active)"
                    Html.td model.LetterString
                ]
            ]
        ]
    ]

let view model dispatch =
    Bulma.container [
        prop.style [ style.border(1, borderStyle.dashed, color.black); style.margin 20; style.padding 20 ]
        prop.children [
            Bulma.title1 "Info Component"

            Bulma.subtitle4 "Different Websocket subscription"

            Bulma.columns [
                Bulma.column [
                    viewStatus dispatch model
                ]
            ]
        ]
    ]

let stream model msgs =
    if model.Remote then
        let websocket =
            AsyncRx.never ()
            |> server
            |> AsyncRx.share

        let letterStringQuery =
            websocket
            |> AsyncRx.choose (function | Shared.Msg.LetterStringChanged str -> Some (LetterStringChanged str) | _ -> None)

        let remote =
            websocket
            |> AsyncRx.map (fun _ -> MsgAdded)
            |> AsyncRx.merge letterStringQuery

        remote
        |> AsyncRx.merge msgs
        |> AsyncRx.tag "remote"

    else
        msgs
        |> AsyncRx.tag "msgs"

let info initialString =
    let initialModel = init initialString
    Reaction.StreamView initialModel view update stream
