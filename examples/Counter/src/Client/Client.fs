module Client

open Fable.PowerPack.Fetch
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Reaction
open Reaction
open Reaction.AsyncObservable

open Shared
open Fulma
open Elmish
open Elmish.React


// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { Counter: Counter option }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Increment
| Decrement
| InitialCountLoaded of Result<Counter, exn>

// defines the initial state and initial command (= side-effect) of the application
let init () : Model =
    { Counter = None }

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model =
    match currentModel.Counter, msg with
    | Some x, Increment ->
        let nextModel = { currentModel with Counter = Some (x + 1) }
        nextModel
    | Some x, Decrement ->
        let nextModel = { currentModel with Counter = Some (x - 1) }
        nextModel
    | _, InitialCountLoaded (Ok initialCount)->
        let nextModel = { Counter = Some initialCount }
        nextModel

    | _ -> currentModel

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

let show = function
| { Counter = Some x } -> string x
| { Counter = None   } -> "Loading..."

let button txt onClick =
    Button.button
        [ Button.IsFullWidth
          Button.Color IsPrimary
          Button.OnClick onClick ]
        [ str txt ]

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [ Navbar.navbar [ Navbar.Color IsPrimary ]
            [ Navbar.Item.div [ ]
                [ Heading.h2 [ ]
                    [ str "SAFE Template" ] ] ]

          Container.container []
              [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ Heading.h3 [] [ str ("Press buttons to manipulate counter: " + show model) ] ]
                Columns.columns []
                    [ Column.column [] [ button "-" (fun _ -> dispatch Decrement) ]
                      Column.column [] [ button "+" (fun _ -> dispatch Increment) ] ] ]

          Footer.footer [ ]
                [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ safeComponents ] ] ]

let loadCountCmd =
    let decoder = Thoth.Json.Decode.int
    ofPromise (fetchAs<int> "/api/init" decoder [])
        |> map (Ok >> InitialCountLoaded)
        |> catch (Error >> InitialCountLoaded >> single)

let query msgs =
    concat [loadCountCmd; msgs]

Program.mkSimple init update view
|> Program.withQuery query
|> Program.withReact "elmish-app"
|> Program.run
