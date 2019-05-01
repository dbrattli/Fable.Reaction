module Info

open Fable.React

open Fulma
open Fulma.Extensions.Wikiki
open Elmish.Streams
open FSharp.Control

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

let update msg model =
    match msg with
    | MsgAdded ->
        { model with Msgs = model.Msgs + 1 }

    | RemoteToggled ->
        { model with Remote = not model.Remote }

    | LetterStringChanged str ->
        { model with LetterString = str }

let viewStatus dispatch model =
    Table.table [ Table.IsHoverable ; Table.IsStriped ] [
        thead [] [
            tr [] [
                th [] [ str "Remote (string and number of messages over websockets)" ]
                th [] [
                    Switch.switch [
                        Switch.Checked model.Remote
                        Switch.OnChange (fun _ -> dispatch RemoteToggled)
                        Switch.Id "remote"
                    ] []
                ]
            ]
        ]

        tbody [] [
            tr [] [
                td [] [ str "Number of remote msgs" ]
                td [] [
                    str <| string model.Msgs
                ]
            ]

            tr [] [
                td [] [ str "Current string (updated when websocket is active)" ]
                td [] [
                    str model.LetterString
                ]
            ]
        ]
    ]

let view model dispatch =
    Container.container [] [
        Heading.h3 [] [ str "Subcomponent 2" ]
        Heading.h4 [ Heading.IsSubtitle ] [ str "Different Websocket subscription" ]
        Columns.columns [] [
            Column.column [] [
                viewStatus dispatch model
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
            |> AsyncRx.toStream "remote"

        Stream.batch [
            remote
            msgs
        ]

    else
        msgs
