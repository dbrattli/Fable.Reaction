module Client

open Fable.Helpers.React
open Fable.Helpers.React.Props

open Reaction.AsyncRx
open Elmish.Reaction
open Elmish
open Elmish.React

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
    | _ -> currentModel

let renderLetter c x y =
    span [ Style [Top y; Left x; Position "absolute"] ] [
        str c
    ]

let view (model : Model) (dispatch : Dispatch<Msg>) =
    let letters = model.Letters

    div [ Style [ FontFamily "Consolas, monospace"; Height "100%"] ] [
        for KeyValue(i, (c, x, y)) in letters do
            yield renderLetter c x y
    ]

let init () : Model =
    { Letters = Map.empty }

// Query for message stream transformation (expression style)
let query model msgs =
    Subscribe (asyncRx {
        let chars =
            Seq.toList "TIME FLIES LIKE AN ARROW"
            |> Seq.mapi (fun i c -> i, c)

        for i, c in chars do
            yield! AsyncRx.ofMouseMove ()
                |> AsyncRx.delay (100 * i)
                |> AsyncRx.map (fun m -> Letter (i, string c, int m.clientX + i * 10 + 15, int m.clientY))
    }, "msgs")

Program.mkSimple init update view
|> Program.withQuery query
|> Program.withReactUnoptimized "elmish-app"
|> Program.run