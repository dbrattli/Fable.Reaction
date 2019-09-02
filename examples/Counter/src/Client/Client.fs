module Client

open Fable.React
open Fable.React.Props
open Fetch.Types
open Fulma
open Thoth.Json

open Shared

open FSharp.Control
open Fable.Reaction

// The application model holds data that you want to keep track of while the
// application is running in this case, we are keeping track of a counter.
// Initially the model will not be available, so we add it together with a case
// that tells if we are currently loading.
type AppModel = { Counter: Counter }

type Model =
    | App of AppModel
    | Loading

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Increment
| Decrement
| InitialCountLoaded of Result<Counter, exn>

// Fetch a data structure from specified url and using the decoder
let fetchWithDecoder<'T> (url: string) (decoder: Decoder<'T>) (init: RequestProperties list) =
    promise {
        let! response = GlobalFetch.fetch(RequestInfo.Url url, Fetch.requestProps init)
        let! body = response.text()
        return Decode.unsafeFromString decoder body
    }

// Inline the function so Fable can resolve the generic parameter at compile time
let inline fetchAs<'T> (url: string) (init: RequestProperties list) =
    // In this example we use Thoth.Json cached auto decoders
    // More info at: https://mangelmaxime.github.io/Thoth/json/v3.html#caching
    let decoder = Decode.Auto.generateDecoderCached<'T>()
    fetchWithDecoder url decoder init

let initialCounter = fetchAs<Counter> "/api/init"

// defines the initial state
let init () : Model =
    Loading

let loadCount =
    AsyncRx.ofPromise (initialCounter [])
    |> AsyncRx.map (Ok >> InitialCountLoaded)
    |> AsyncRx.catch (Error >> InitialCountLoaded >> AsyncRx.single)

let stream  model msgs =
    printf "stream"

    match model with
    | Loading -> loadCount |> AsyncRx.tag "loading"
    | _ -> msgs |> AsyncRx.tag "msgs"

// The update function computes the next state of the application based on the current state and the incoming events/messages
let update (currentModel : Model) (msg : Msg) : Model =
    printf "update: %A" msg

    match currentModel, msg with
    | Loading, InitialCountLoaded (Ok initialCount) ->
        App { Counter = initialCount }
    | App model, msg ->
        match model.Counter, msg with
        | counter, Increment ->
            App { model with Counter = { Value = counter.Value + 1 } }
        | counter, Decrement ->
            App { model with Counter = { Value = counter.Value - 1 } }
        | _ -> currentModel
    | _ -> currentModel


let safeComponents =
    let components =
        span [ ]
           [
             a [ Href "https://saturnframework.github.io" ] [ str "Saturn" ]
             str ", "
             a [ Href "http://fable.io" ] [ str "Fable" ]
             str ", "
             a [ Href "https://elmish.github.io/elmish/" ] [ str "Elmish" ]
             str ", "
             a [ Href "https://fulma.github.io/Fulma" ] [ str "Fulma" ]
             str ", "
             a [ Href "http://elmish-streams.rtfd.io/" ] [ str "Elmish.Streams" ]
           ]

    span [ ]
        [ strong [] [ str "SAFE Template" ]
          str " powered by: "
          components ]

let show = function
| App { Counter = counter } -> string counter.Value
| Loading -> "Loading..."

let button txt onClick =
    Button.button [ Button.IsFullWidth
                    Button.Color IsPrimary
                    Button.OnClick onClick ] [
        str txt ]

let view (model : Model) (dispatch : (Msg -> unit)) =
    printf "view: %A" model
    div [] [
        Navbar.navbar [ Navbar.Color IsPrimary ] [
            Navbar.Item.div [] [
                Heading.h2 [] [
                    str "SAFE Template"
                ]
            ]
        ]

        Container.container [] [
            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ]] [
                Heading.h3 [] [
                    str ("Press buttons to manipulate counter: " + show model)
                ]
            ]
            Columns.columns [] [
                Column.column [] [
                    button "-" (fun _ -> dispatch Decrement)
                ]
                Column.column [] [
                    button "+" (fun _ -> dispatch Increment)
                ]
            ]
        ]

        Footer.footer [] [
            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ]] [
                safeComponents
            ]
        ]
    ]

printf "Starting program"
let initialModel = init ()
let app = Reaction.mvuStream initialModel view update stream

mountById "app" (ofFunction app () [])
