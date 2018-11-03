module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Thoth.Json

open Shared

open Fulma

open Fable.Reaction
open Fable.Reaction.WebSocket
open Reaction
open Shared


type LetterSource =
 | None
 | Local of Map<int,LetterPos>
 | Remote of Map<int,LetterPos>


type AppModel =
    {
        Counter: Counter
        Letters : LetterSource
    }

type Model =
    | Loading
    | CounterAvailable of AppModel

type Msg =
    | Increment
    | Decrement
    | InitialCountLoaded of Result<Counter, exn>
    | Letter of int * LetterPos
    | RemoteMsg of Shared.Msg

let init () : Model =
    Loading


let loadCountCmd () =
    ofPromise (fetchAs<int> "/api/init" Decode.int [])
        |> AsyncObservable.map (Ok >> InitialCountLoaded)
        |> AsyncObservable.catch (Error >> InitialCountLoaded >> AsyncObservable.single)


let withLetterSource model =
    if model.Counter > 50 then
        match model.Letters with
        | None ->
            { model with Letters = Remote Map.empty }

        | Remote _ ->
            model

        | Local letters ->
            { model with Letters = Remote letters }

    elif model.Counter > 45 then
        match model.Letters with
        | None ->
            { model with Letters = Local Map.empty }

        | Remote letters ->
            { model with Letters = Local letters }

        | Local _ ->
            model

    else  { model with Letters = None }



// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (model : Model) : Model =

    match model, msg with
    | CounterAvailable appModel, Increment ->
        Fable.Import.Browser.console.log appModel.Counter
        Fable.Import.Browser.console.log msg
        { appModel with Counter = appModel.Counter + 1 }
        |> withLetterSource
        |> CounterAvailable

    | CounterAvailable appModel, Decrement ->
        Fable.Import.Browser.console.log msg
        { appModel with Counter = appModel.Counter - 1 }
        |> withLetterSource
        |> CounterAvailable

    | Loading, InitialCountLoaded (Ok initialCount)->
        { Counter = initialCount ; Letters = None }
        |> withLetterSource
        |> CounterAvailable

    | CounterAvailable appModel, RemoteMsg (Shared.Letter (index, pos)) ->
        match appModel.Letters with
        | Remote letters ->
            CounterAvailable { appModel with Letters = Remote <| letters.Add (index, pos) }

        | _ -> CounterAvailable appModel


    | CounterAvailable appModel, Letter (index, pos) ->
        match appModel.Letters with
        | Local letters ->
            CounterAvailable { appModel with Letters = Local <| letters.Add (index, pos) }

        | _ -> CounterAvailable appModel


    | _ -> model


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
| CounterAvailable { Counter = x } -> string x
| Loading -> "Loading..."

let button txt onClick =
    Button.button
        [ Button.IsFullWidth
          Button.Color IsPrimary
          Button.OnClick onClick ]
        [ str txt ]

let offsetX x i = (int x) + i * 10 + 15

let viewLetters model =
    match model with
    | Loading ->
        div [] [ str "Initial Counter not loaded" ]

    | CounterAvailable appModel ->
        match appModel.Letters with
        | None ->
              str ""

        | Remote letters | Local letters ->
            div []
                [ for KeyValue(i, pos) in letters do
                    yield span [ Style [Top pos.Y; Left (offsetX pos.X i); Position "absolute"] ]
                        [ str pos.Letter ] ]

    |> List.singleton
    |> div [ Style [ FontFamily "Consolas, monospace"; FontWeight "Bold"; Height "100%"] ]


let letterSubscription appModel =
    match appModel.Letters with
    | Remote letters | Local letters ->
        "yes"

    | _ ->
        "no"

let letterSubscriptionOverWebsockets appModel =
    match appModel.Letters with
    | Remote letters ->
        "yes"

    | _ ->
        "no"



let viewStatus model =
    match model with
    | Loading ->
        div [] [ str "Initial Counter not loaded" ]

    | CounterAvailable appModel ->
        Table.table [ Table.IsHoverable ; Table.IsStriped ]
            [
                thead [ ]
                    [
                        tr [ ]
                            [
                                th [ ] [ str "Feature" ]
                                th [ ] [ str "Active" ]
                            ]
                    ]

                tbody [ ]
                    [ tr [ ]
                         [ td [ ] [ str "Letter Subscription (counter > 45)" ]
                           td [ ] [ str <| letterSubscription appModel ] ]
                      tr [  ]
                         [ td [ ] [ str "Letters over Websockets (counter > 50)" ]
                           td [ ] [ str <| letterSubscriptionOverWebsockets appModel ] ] ]
              ]

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [
          Navbar.navbar [ Navbar.Color IsPrimary ]
            [ Navbar.Item.div [ ]
                [ Heading.h2 [ ]
                    [ str "SAFE Template with Fable.Reaction" ] ] ]

          Container.container []
              [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ Heading.h4 [] [ str ("Counter must be > 45 to release magic. Current: " + show model) ] ]
                Columns.columns []
                    [
                        Column.column [] [ button "-" (fun _ -> dispatch Decrement) ]
                        Column.column [] [ button "+" (fun _ -> dispatch Increment) ]
                    ]

                Columns.columns []
                    [ Column.column [] [ viewStatus model ] ]

                Columns.columns []
                    [ Column.column [] [ viewLetters model ] ] ]



          Footer.footer [ ]
                [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ safeComponents ] ] ]

let server source =
    msgChannel<Shared.Msg>
        "ws://localhost:8085/ws"
        Shared.Msg.Encode
        Shared.Msg.Decode
        source

let letterStream =
    "Magic - Released!"
    |> Seq.toList // Split into list of characters
    |> Seq.mapi (fun i c -> i, c) // Create a tuple with the index
    |> AsyncObservable.ofSeq // Make this an observable
    |> AsyncObservable.flatMap (fun (i, letter) ->
        ofMouseMove ()
        |> AsyncObservable.delay (100 * i)
        |> AsyncObservable.map (fun ev -> (i, { Letter= string letter; X=ev.clientX; Y=ev.clientY}))
    )

let query (model : Model) msgs =
    match model with
    | CounterAvailable appModel ->
        match appModel.Letters with
        | Local letters ->
            letterStream
            |> AsyncObservable.map Letter
            |> AsyncObservable.merge msgs
            ,"local"

         | Remote _ ->
            letterStream
            |> AsyncObservable.map Shared.Msg.Letter
            |> server
            |> AsyncObservable.map RemoteMsg
            |> AsyncObservable.merge msgs
            ,"remote"

        | _ ->
              msgs,"none"

    | Loading ->
        loadCountCmd (),"loading"



#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkSimple init update view
|> Program.withQuery query
#if DEBUG
// |> Program.withConsoleTrace
// |> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
