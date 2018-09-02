module Client

open Fable.Core
open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch
open Fable.Reaction

open Fulma
open Reaction
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
let update (currentModel : Model) (msg : Msg) : Model =
    printfn "Model: %A" (currentModel, msg)
    let model =
        match msg with
        | QueryResult res ->
            match res with
            | Ok lists ->
                { Result = lists.[1]; Loading = false }
            | _ -> currentModel
        | Loading ->
            { currentModel with Loading = true}
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


let view (dispatch : Msg -> unit) (model : Model) =
    div [] [
        Navbar.navbar [ Navbar.Color IsPrimary ] [
            Navbar.Item.div [ ] [
                Heading.h2 [ ] [
                    str "Fable Reaction Autocomplete Example" ]
            ]
        ]

        Container.container [] [
            h1 [] [
                str "Search Wikipedia"
            ]

            div [ Class "dropdown is-active" ] [
                div [ Class "dropdown-trigger" ] [
                    input [ Placeholder "Enter query ..."
                            OnKeyUp (KeyboardEvent >> dispatch)
                            Class "input"]
                ]
                div [ Class "dropdown-menu"
                      Id "dropdown-menu"
                      Role "menu" ] [
                    div [ Class "dropdown-content" ] [
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

open Fable.PowerPack

let ofPromise (pr: Fable.Import.JS.Promise<_>) =
    let obv = Creation.ofAsync(fun obv _ -> async {
        try
            let! result = Async.AwaitPromise pr
            do! OnNext result |> obv
            do! OnCompleted |> obv
        with
        | ex ->
            printfn "ofPromise: got error: %s" (ex.ToString ())
            do! OnError ex |> obv
    })
    AsyncObservable obv

let searchWikipedia (term : string) =
    let jsonDecode txt =
        let decoders = Decode.oneOf [ Decode.list Decode.string; (Decode.succeed []) ]
        Decode.decodeString (Decode.list decoders) txt

    let url = sprintf "http://en.wikipedia.org/w/api.php?action=opensearch&origin=*&format=json&search=%s" term
    let props = [
        RequestProperties.Mode RequestMode.Cors
        requestHeaders [ ContentType "application/json" ]
    ]

    if term.Length = 0 then
        QueryResult (Ok [[];[];[]]) |> single
    else
        ofPromise (fetch url props)
        |> flatMap (fun res -> res.text() |> ofPromise)
        |> map jsonDecode
        |> map QueryResult
        |> catch (sprintf "%A" >> Error >> QueryResult >> single)

let query (msgs: AsyncObservable<Msg>) =
    let targetValue (ev : Fable.Import.React.KeyboardEvent) : string =
        (unbox<string> ev.target?value).Trim()

    let terms =
        msgs
        |> choose Msg.asKeyboardEvent
        |> map targetValue
        |> filter (fun term -> term.Length > 2 || term.Length = 0)
        |> debounce(750)  // Pause for 750ms
        |> distinctUntilChanged  // Only if the value has changed

    terms
    |> flatMapLatest searchWikipedia

Program.mkProgram init update view
|> Program.withMsgs query
|> Program.withReact "elmish-app"
|> Program.run
