=============================
Examples using Elmish Streams
=============================

Examples of how to use Elmish Streams can be found in the
examples folder. Current list of examples includes:

* `Counter
  <https://github.com/dbrattli/Fable.Reaction/blob/master/examples/Counter/src/Client/Client.fs>`_,
  from the `SAFE <https://safe-stack.github.io/>`_ stack template.

* `Timeflies
  <https://github.com/dbrattli/Fable.Reaction/blob/master/examples/Timeflies/src/Client/Client.fs>`_.
  See description below.

* `Autocomplete
  <https://github.com/dbrattli/Fable.Reaction/tree/master/examples/Autocomplete>`_

The Timeflies example (`source code
<https://github.com/dbrattli/Re-action/tree/master/examples/Timeflies))
implements the classic [Time
Flies](https://blogs.msdn.microsoft.com/jeffva/2010/03/17/reactive-extensions-for-javascript-the-time-flies-like-an-arrow-sample/>`_)
example from `RxJS <https://rxjs-dev.firebaseapp.com/>`_.

.. code:: fsharp

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
    let query (msgs : IAsyncObservable<Msg>) : IAsyncObservable<Msg>) =
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
