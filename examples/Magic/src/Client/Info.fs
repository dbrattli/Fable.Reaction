module Info

open Fable.React
open Fable.React.Props

open Fulma
open Fulma.Extensions.Wikiki
open Fable.Reaction
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

let update model msg =
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
                        Switch.Id "remoteInfo"
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
    Container.container [ Container.Option.Props [ Style [ Border "1px dashed"; Margin "20px"; Padding "20px" ]]] [
        Heading.h3 [] [ str "Info Component" ]
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

        remote
        |> AsyncRx.merge msgs
        |> AsyncRx.tag "remote"

    else
        msgs
        |> AsyncRx.tag "msgs"

let info initialString =
    let initialModel = init initialString

    FunctionComponent.Of(fun () ->
        let model = Hooks.useReducer(update, initialModel)
        let dispatch, msgs = Reaction.useStatefulStream(model.current, model.update, stream)

        (*
        let select =
            msgs
            |> AsyncRx.map (fun x -> x.ToString ())
        //select.Run (OnNext >> obv)
*)
        view model.current dispatch
    )

