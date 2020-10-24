module Client

open Fable.React
open Fable.React.Props
open Elmish
open Elmish.React
open Fulma

open AutoComplete
open Thoth.Json
open Fetch

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = {
    Selection: string option
}

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    | Select of string

let init () : Model = {
    Selection = None
}

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg: Msg) (currentModel: Model)  : Model =
    let model =
        match msg with
        | Select entry ->
            { currentModel with Selection = Some entry }
    model

let safeComponents =
    let components =
        span [ ]
           [
             a [ Href "https://github.com/giraffe-fsharp/Giraffe" ] [ str "Giraffe" ]
             str ", "
             a [ Href "http://fable.io" ] [ str "Fable" ]
             str ", "
             a [ Href "https://mangelmaxime.github.io/Fulma" ] [ str "Fulma" ]
             str ", "
             a [ Href "https://github.com/dbrattli/Fable.Reaction" ] [ str "Reaction" ]
           ]

    p [ ]
        [ strong [] [ str "SAFE Template" ]
          str " powered by: "
          components ]


let searchWikipedia (term: string) =
    promise {
        let jsonDecode txt =
            let decoders = Decode.oneOf [ Decode.list Decode.string; (Decode.succeed []) ]
            Decode.fromString (Decode.list decoders) txt

        let url = sprintf "https://en.wikipedia.org/w/api.php?action=opensearch&origin=*&format=json&search=%s" term
        let props = [
            RequestProperties.Mode RequestMode.Cors
            requestHeaders [ ContentType "application/json" ]
        ]

        if term.Length = 0 then
            return Ok []
        else
            let! response = fetch url props
            let! json = response.text ()
            let result = jsonDecode json |> Result.map (fun x -> x.[1])
            return result
    }

let view (model: Model) (dispatch : Dispatch<Msg>) =
    div [] [
        Navbar.navbar [ Navbar.Color IsPrimary ] [
            Navbar.Item.div [] [
                Heading.h2 [] [
                    str "Autocomplete"
                ]
            ]
        ]

        Container.container [ Container.Props [Style [ PaddingTop 40; PaddingBottom 150 ]]] [
            h1 [] [
                str "Search Wikipedia"
            ]

            autocomplete { Search=searchWikipedia; Dispatch = Select >> dispatch; DebounceTimeout=750 }

            div [ Style [ MarginTop "30px" ]] [
                match model.Selection with
                | Some selection ->
                    yield str "Selection: "
                    yield str selection
                | None -> ()
            ]
        ]

        Footer.footer [ ] [
            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] [
                safeComponents
            ]
        ]
    ]

Program.mkSimple init update view
|> Program.withReactBatched "elmish-app"
|> Program.run