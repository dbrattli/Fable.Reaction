module Client

open Fable.React
open Fable.React.Props
open Fable.Core
open Reaction
open Elmish
open Elmish.React
open Browser.Dom

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
                yield span [ Key (c + string i); Style [Top y; Left x; Position (PositionOptions.Fixed)] ]
                    [ str c ] ] |> ofList
        ]

let init () : Model =
    { Letters = Map.empty }

let getOffset (element: Browser.Types.Element) =
    let doc = element.ownerDocument
    let docElem = doc.documentElement
    let clientTop  = docElem.clientTop
    let clientLeft = docElem.clientLeft
    let scrollTop  = window.pageYOffset
    let scrollLeft = window.pageXOffset

    int (scrollTop - clientTop), int (scrollLeft - clientLeft)

let container = document.querySelector "#elmish-app"
let top, left = getOffset container

// Query for message stream transformation (expression style)
let query (model : Model) (msgs:  Stream<Msg, string>) =
    asyncRx {
        let chars =
            Seq.toList "TIME FLIES LIKE AN ARROW"
            |> Seq.mapi (fun i c -> i, c)

        let! i, c = AsyncRx.ofSeq chars
        yield! AsyncRx.ofMouseMove ()
            |> AsyncRx.delay (100 * i)
            |> AsyncRx.map (fun m -> Letter (i, string c, int m.clientX + i * 10 + 15 - left, int m.clientY - top))
    } |> AsyncRx.toStream "msgs"

Program.mkSimple init update view
|> Program.withMsgStream query "msgs"
|> Program.withReact "elmish-app"
|> Program.run