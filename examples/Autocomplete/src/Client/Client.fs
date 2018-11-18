module Client

open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch
open Reaction
open Elmish
open Elmish.React
open Fulma
open Thoth.Json

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { Result: string list; Loading: bool }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    | KeyboardEvent of Fable.Import.React.KeyboardEvent
    | Loading
    | QueryResult of Result<string list list, string>
        static member EmptyResult = [[];[];[]]
        static member asKeyboardEvent = function
            | KeyboardEvent ev -> Some ev
            | _ -> None

// defines the initial state and initial command (= side-effect) of the application
let init () : Model = {
    Result = []
    Loading = false
}

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg: Msg) (currentModel: Model) : Model =
    let model =
        match msg with
        | QueryResult res ->
            match res with
            | Ok lists ->
                { Result = lists.[1]; Loading = false }
            | _ -> { currentModel with Loading = false }
        | Loading ->
            { currentModel with Loading = true }
        | _ -> currentModel
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

            Dropdown.dropdown [ active model.Result ] [
                div [] [
                    div [ Class ("control " + loading model.Loading) ] [
                        Input.input [ Input.Option.Placeholder "Enter query ..."
                                      Input.Option.Props [ OnKeyUp (KeyboardEvent >> dispatch)]
                                    ]
                    ]
                ]

                Dropdown.menu [ GenericOption.Props [ Role "menu" ]] [
                    Dropdown.content [] [
                        for item in model.Result do
                            yield
                                a [ Href "#"
                                    Class "dropdown-item" ] [
                                    str item ]
                    ]
                ]
            ]
        ]

        Footer.footer [ ] [
            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] [
                safeComponents
            ]
        ]
    ]

let searchWikipedia (term: string) =
    let jsonDecode txt =
        let decoders = Decode.oneOf [ Decode.list Decode.string; (Decode.succeed []) ]
        Decode.fromString (Decode.list decoders) txt

    let url = sprintf "http://en.wikipedia.org/w/api.php?action=opensearch&origin=*&format=json&search=%s" term
    let props = [
        RequestProperties.Mode RequestMode.Cors
        requestHeaders [ ContentType "application/json" ]
    ]

    if term.Length = 0 then
        QueryResult (Ok Msg.EmptyResult) |> AsyncRx.single
    else
        AsyncRx.ofPromise (fetch url props)
        |> AsyncRx.flatMap (fun res -> res.text () |> AsyncRx.ofPromise)
        |> AsyncRx.map jsonDecode
        |> AsyncRx.map QueryResult
        |> AsyncRx.catch (sprintf "%A" >> Error >> QueryResult >> AsyncRx.single)

let query model msgs =
    let targetValue (ev: Fable.Import.React.KeyboardEvent) : string =
        try
            let target = !!ev.target?value : string
            target.Trim ()
        with _ -> ""

    let terms =
        msgs
        |> AsyncRx.choose Msg.asKeyboardEvent
        |> AsyncRx.map targetValue       // Map keyboard event to input value
        |> AsyncRx.filter (fun term -> term.Length > 2 || term.Length = 0)
        |> AsyncRx.debounce 750          // Pause for 750ms
        |> AsyncRx.distinctUntilChanged  // Only if the value has changed

    Queries [
        let loading =
            terms
            |> AsyncRx.filter (fun x -> x.Length > 0)
            |> AsyncRx.map (fun _ -> Loading)
        yield Query (loading, "loading")

        let results =
            terms
            |> AsyncRx.flatMapLatest searchWikipedia
        yield Query (results, "search")
    ]

Program.mkSimple init update view
|> Program.withQuery query
|> Program.withReact "elmish-app"
|> Program.run
