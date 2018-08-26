# fa·ble / re·ac·tion

Fable.Reaction is a lightweight Async Reactive ([Rx](http://reactivex.io/)) [Elmish](https://elmish.github.io/)-ish library for F# targeting [Fable](http://fable.io/) and [React](https://reactjs.org/).

Currently a playground project for experimenting with MVU-based web applications using async reactive functional programming (Async Observables) in F#. The project is heavily inspired by [Elm](http://elm-lang.org/) and [Elmish](https://elmish.github.io/) but currently a separate project.

## Elmish Reaction example

Reactive [MVU archtecture](https://guide.elm-lang.org/architecture/) example ([source code](https://github.com/dbrattli/Re-action/tree/master/examples/Timeflies)) using Reaction for implementing the classic Time Flies example from RxJS. This code
is very simplar to Elmish but the difference is that we can compose powerful reactive
queries to transform, filter, aggregate and time-shift our stream of messages.

```f#
// Messages for updating the model
type Msg =
    | Letter of int * string * int * int

// Model (state) for updating the view
type Model = {
    Letters: Map<int, string * int * int>
}

let update (currentModel : Model) (msg : Msg) : Model =
    match currentModel.Letters, msg with
    | _, Letter (i, c, x, y) ->
        { currentModel with Letters = currentModel.Letters.Add (i, (c, x, y)) }

// View for being observed by React
let view (model : Model) =
    let letters = model.Letters
    let offsetX x i = x + i * 10 + 15

    div [ Style [ FontFamily "Consolas, monospace"]] [
        for KeyValue(i, (c, x, y)) in letters do
            yield span [ Style [Top y; Left (offsetX x i); Position "absolute"] ] [
                str c
            ]
    ]

let main = async {
    let initialModel = { Letters = Map.empty }

    let moves =
        Seq.toList "TIME FLIES LIKE AN ARROW" |> Seq.map string |> ofSeq
            |> flatMapi (fun (x, i) ->
                fromMouseMoves ()
                    |> map (fun m -> Letter (i, x, int m.clientX, int m.clientY))
                    |> delay (100 * i)
            )
            |> scan initialModel update
            |> map view

    // React observer
    let obv = renderReact "elmish-app"

    // Subscribe (ignores the disposable)
    do! moves.RunAsync obv
}
```
