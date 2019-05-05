module Client

open Fable.React
open Fable.React.Props
open Elmish
open Elmish.React
open Elmish.Streams
open Fulma

open Components

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { AutoComplete : AutoComplete.Model }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    | AutoCompleteMsg of AutoComplete.Msg

let asAutoCompleteMsg = function
    | AutoCompleteMsg autoCompleteMsg -> Some autoCompleteMsg
    | _ -> None

// defines the initial state and initial command (= side-effect) of the application
let init () : Model = {
    AutoComplete = AutoComplete.init ()
}

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg: Msg) (currentModel: Model) : Model =
    let model =
        match msg with
        | AutoCompleteMsg autoMsg ->
            { currentModel with AutoComplete = AutoComplete.update autoMsg currentModel.AutoComplete }
    model

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


let view (model: Model) (dispatch : Msg -> unit) =
    let active (result : string list) =
        Dropdown.Option.IsActive (result.Length > 0)

    let loading (loading: bool) =
        if loading then
            "is-loading"
        else
            ""

    div [] [
        Navbar.navbar [ Navbar.Color IsPrimary ] [
            Navbar.Item.div [ ] [
                Heading.h2 [ ] [
                    str "Autocomplete" ]
            ]
        ]

        Container.container [ Container.Props [Style [ CSSProp.PaddingTop 40; CSSProp.PaddingBottom 150 ]]] [
            h1 [] [
                str "Search Wikipedia"
            ]

            AutoComplete.view model.AutoComplete (AutoCompleteMsg >> dispatch)
        ]

        Footer.footer [ ] [
            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] [
                safeComponents
            ]
        ]
    ]

let stream model msgs =
    msgs
    |> Stream.subStream AutoComplete.stream model.AutoComplete asAutoCompleteMsg AutoCompleteMsg "auto-complete"

Program.mkSimple init update view
|> Program.withStream stream "msgs"
|> Program.withReactBatched "elmish-app"
|> Program.run
