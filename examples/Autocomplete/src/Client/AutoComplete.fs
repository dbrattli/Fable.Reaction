namespace Components

open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open FSharp.Control
open Elmish.Streams
open Fulma
open Thoth.Json
open Fetch

module AutoComplete =

    // The model holds data that you want to keep track of while the application is running
    // in this case, we are keeping track of a counter
    // we mark it as optional, because initially it will not be available from the client
    // the initial value will be requested from server
    type Model = { Result: string list; Loading: bool }

    // The Msg type defines what events/actions can occur while the application is running
    // the state of the application changes *only* in reaction to these events
    type Msg =
        | KeyboardEvent of Browser.Types.KeyboardEvent
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

    let view (model: Model) (dispatch : Msg -> unit) =
        let active (result : string list) =
            Dropdown.Option.IsActive (result.Length > 0)

        let loading (loading: bool) =
            if loading then
                "is-loading"
            else
                ""


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

    let searchWikipedia (term: string) =
        let jsonDecode txt =
            let decoders = Decode.oneOf [ Decode.list Decode.string; (Decode.succeed []) ]
            Decode.fromString (Decode.list decoders) txt

        let url = sprintf "https://en.wikipedia.org/w/api.php?action=opensearch&origin=*&format=json&search=%s" term
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

    let stream model msgs =
        let targetValue (ev: Browser.Types.KeyboardEvent) : string =
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

        Stream.batch [
            terms
            |> AsyncRx.filter (fun x -> x.Length > 0)
            |> AsyncRx.map (fun _ -> Loading)
            |> AsyncRx.toStream "loading"

            terms
            |> AsyncRx.flatMapLatest searchWikipedia
            |> AsyncRx.toStream "search"
        ]
