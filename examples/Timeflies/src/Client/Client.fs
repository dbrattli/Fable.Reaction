module Client

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Import
open Reaction
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

let view (model : Model) (dispatch : Dispatch<Msg>) =
    let letters = model.Letters

    div [ Style [ FontFamily "Consolas, monospace"; Height "100%"] ]
        [
            [ for KeyValue(i, (c, x, y)) in letters do
                yield span [ Key (c + string i); Style [Top y; Left x; Position "fixed"] ]
                    [ str c ] ] |> ofList
        ]

let init () : Model =
    { Letters = Map.empty }

let getOffset (element: Browser.Element) =
    let doc = element.ownerDocument
    let docElem = doc.documentElement
    let body = doc.body
    let clientTop  = docElem.clientTop
    let clientLeft = docElem.clientLeft
    let scrollTop  = Browser.window.pageYOffset
    let scrollLeft = Browser.window.pageXOffset

    int (scrollTop - clientTop), int (scrollLeft - clientLeft)

let container = Browser.document.querySelector "#elmish-app"
let top, left = getOffset container

// Query for message stream transformation (expression style)
let query msgs =
    asyncRx {
        let chars =
            Seq.toList "TIME FLIES LIKE AN ARROW"
            |> Seq.mapi (fun i c -> i, c)

        let! i, c = AsyncRx.ofSeq chars
        yield! AsyncRx.ofMouseMove ()
            |> AsyncRx.delay (100 * i)
            |> AsyncRx.map (fun m -> Letter (i, string c, int m.clientX + i * 10 + 15 - left, int m.clientY - top))
    }

Program.mkSimple init update view
|> Program.withSimpleQuery query
|> Program.withReact "elmish-app"
|> Program.run