# Fable Reaction

Fable Reaction is a helper library for using the [Reaction](https://github.com/dbrattli/Reaction) Async Reactive ([Rx](http://reactivex.io/)) library with Fable and [Elmish](https://elmish.github.io/).

## Install

```cmd
paket add Fable.Reaction --version 0.4.0
```

## Reactive MVU Architecture

Fable Reaction is very similar to [Elm](http://elm-lang.org/) and [Elmish](https://elmish.github.io/) in regards to the [MVU architecture](https://guide.elm-lang.org/architecture/). But when using Fable Reaction, we do not need any commands (`Cmd`) or subscriptions with Elmish. Instead we use a [ReactiveX](http://reactivex.io/) (Rx) style query that transforms the stream of messages (`Msg`).

<img src="R-MVU.png" width="400">

* **Model**, application state as immutable data
* **View**, a pure function that takes the model to produce the output view (HTML elements)
* **Message**, a data event that represents a change. Messages are generated by the view, or they may be generated by the reaction query, e.g. timer or initial message events.
* **Update**, a pure function that produces a new model based on a received message and the previous model

In addition, Fable Reaction may also have a reaction query that transforms the "stream" of messages.

* **Reaction**, a query function that takes the message stream and produces a new (transformed) message stream. Note that this also replaces Elm(ish) commands (Cmd) since the reaction is free to produce initial messages out of thin air, transform, filter, time-shift messages or combine side-effects such as web requests (fetch) etc.

## Howto use with Elmish

```f#
open Reaction        // 1. Open Reaction, for operators such as delay.
open Fable.Reaction  // 2. Open Fable.Reaction

// (your Elmish program here)

let query msgs = // 3. Add reactive query
    msgs |> delay 1000

Program.mkSimple init update view
|> Program.withQuery query       // 4. Enable the query in Elmish
|> Program.withReact "elmish-app"
|> Program.run
```

## Elmish Examples

Examples of how to use Fable Reaction with Elmish can be found in the examples folder. Current list of examples includes:

* [Counter](https://github.com/dbrattli/Fable.Reaction/blob/master/examples/Counter/src/Client/Client.fs), from the [SAFE](https://safe-stack.github.io/) stack template.
* [Timeflies](https://github.com/dbrattli/Fable.Reaction/blob/master/examples/Timeflies/src/Client/Client.fs). See description below.
* [Autocomplete](https://github.com/dbrattli/Fable.Reaction/tree/master/examples/Autocomplete)

The Timeflies example ([source code](https://github.com/dbrattli/Re-action/tree/master/examples/Timeflies)) implements the classic [Time Flies](https://blogs.msdn.microsoft.com/jeffva/2010/03/17/reactive-extensions-for-javascript-the-time-flies-like-an-arrow-sample/) example from [RxJS](https://rxjs-dev.firebaseapp.com/).

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
let update (msg : Msg) (currentModel : Model) : Model =
    match currentModel.Letters, msg with
    | _, Letter (i, c, x, y) ->
        { currentModel with Letters = currentModel.Letters.Add (i, (c, x, y)) }

let view (dispatch : Dispatch<Msg>) (model : Model)  =
    let letters = model.Letters
    let offsetX x i = x + i * 10 + 15

    div [ Style [ FontFamily "Consolas, monospace"]] [
        for KeyValue(i, (c, x, y)) in letters do
            yield span [ Style [Top y; Left (offsetX x i); Position "absolute"] ] [
                str c
            ]
    ]

let init () : Model = { Letters = Map.empty }

// Message stream transformation using Reaction
let query (msgs : AsyncObservable<Msg>) : AsyncObservable<Msg>) =
    rx {
        let! i, c = Seq.toList "TIME FLIES LIKE AN ARROW"
                    |> Seq.mapi (fun i c -> i, c)
                    |> ofSeq

        let ms = fromMouseMoves () |> delay (100 * i)
        for m in ms do
            yield Letter (i, string c, int m.clientX, int m.clientY)
    }

Program.mkSimple init update view
|> Program.withQuery query
|> Program.withReact "elmish-app"
|> Program.run
```
