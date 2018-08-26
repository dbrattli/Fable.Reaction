# Fable Reaction

Fable Reaction is a lightweight Async Reactive ([Rx](http://reactivex.io/)) [Elmish](https://elmish.github.io/)-ish library for F# targeting [Fable](http://fable.io/) and [React](https://reactjs.org/).

Currently a playground project for experimenting with MVU-based web applications using async reactive functional programming (Async Observables) in F#. The project is heavily inspired by [Elm](http://elm-lang.org/) and [Elmish](https://elmish.github.io/) but currently a separate project.

The difference from Elmish and Elm is that Fable.Reaction does not need any commands (`Cmd`) or subscriptions. Instead we use an ReactiveX (Rx) style query that transforms the stream of messages (`Msg`).

## Fable Reaction example

Reactive [MVU archtecture](https://guide.elm-lang.org/architecture/) example ([source code](https://github.com/dbrattli/Re-action/tree/master/examples/Timeflies)) using Reaction for implementing the classic Time Flies example from RxJS. This code
is very simplar to Elmish but the difference is that we can compose powerful reactive
queries to transform, filter, aggregate and time-shift our stream of messages.

```f#
// The model holds data that you want to keep track of while the
// application is running
type Model = {
    Letters: Map<int, string * int * int>
}

// The Msg type defines what events/actions can occur while the
// application is running. The state of the application changes *only*
// in reaction to these events
type Msg =
    | Letter of int * string * int * int

// The update function computes the next state of the application based
// on the current state and the incoming messages
let update (currentModel : Model) (msg : Msg) : Model =
    match currentModel.Letters, msg with
    | _, Letter (i, c, x, y) ->
        { currentModel with Letters = currentModel.Letters.Add (i, (c, x, y)) }

let view (model : Model) (dispatch : Dispatch<Msg>) =
    let letters = model.Letters
    let offsetX x i = x + i * 10 + 15

    div [ Style [ FontFamily "Consolas, monospace"]] [
        for KeyValue(i, (c, x, y)) in letters do
            yield span [ Style [Top y; Left (offsetX x i); Position "absolute"] ] [
                str c
            ]
    ]

let init () : Model =
    { Letters = Map.empty }

// Query for message stream transformation.
let query msgs = rx {
    let! i, c = Seq.toList "TIME FLIES LIKE AN ARROW"
                |> Seq.mapi (fun i c -> i, c)
                |> ofSeq

    let ms = fromMouseMoves () |> delay (100 * i)
    for m in ms do
        yield Letter (i, string c, int m.clientX, int m.clientY)
}

Program.mkReaction init update view
|> Program.withRx query
|> Program.withReact "elmish-app"
|> Program.run
```
