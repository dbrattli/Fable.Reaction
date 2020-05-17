module Client

open Browser.Dom
open Fable.React
open Fable.Reaction
open FSharp.Control
open Feliz
open System

// The model holds data that you want to keep track of while the
// application is running
type Model = {
    Letters: Map<int, string * int * int>
    Fps: int
    Second: int64
    Count: int
}

// The Msg type defines what events/actions can occur while the
// application is running. The state of the application changes *only*
// in reaction to these events
type Msg =
    | Letter of int * string * int * int

// The update function computes the next state of the application based
// on the current state and the incoming messages
let update (currentModel : Model) (msg : Msg) : Model =
    let second = DateTimeOffset.Now.ToUnixTimeSeconds ()
    match msg with
    | Letter (i, c, x, y) ->
        { currentModel with
            Letters = currentModel.Letters.Add (i, (c, x, y))
            Second = second
            Fps = if second > currentModel.Second then currentModel.Count else currentModel.Fps
            Count = if second > currentModel.Second then 0 else currentModel.Count + 1
        }

let view (model : Model) (dispatch : Msg -> unit)=
    let letters = model.Letters

    Html.div [
        prop.style [
            style.fontFamily "Consolas, monospace"
            style.height 100
        ]
        prop.children [
            for KeyValue(i, (c, x, y)) in letters do
                Html.span [
                    prop.key (c + string i)
                    prop.style [
                        style.top y
                        style.left x
                        style.position.fixedRelativeToWindow
                    ]
                    prop.text c
                ]
            Html.span ("Fps: " + (string model.Fps))
        ]
    ]

let initialModel : Model =
    {
        Letters = Map.empty
        Count = 0
        Second = 0L
        Fps = 0
    }

let getOffset (element: Browser.Types.Element) =
    let doc = element.ownerDocument
    let docElem = doc.documentElement
    let clientTop  = docElem.clientTop
    let clientLeft = docElem.clientLeft
    let scrollTop  = window.pageYOffset
    let scrollLeft = window.pageXOffset

    int (scrollTop - clientTop), int (scrollLeft - clientLeft)

let container = document.querySelector "#reaction-app"
let top, left = getOffset container

// Message stream transformation (expression style)
let stream (model : Model) (msgs: IAsyncObservable<Msg>) =
    asyncRx {
        let chars =
            Seq.toList "TIME FLIES LIKE AN ARROW"
            |> Seq.mapi (fun i c -> i, c)

        let! i, c = AsyncRx.ofSeq chars
        yield! AsyncRx.ofMouseMove ()
            |> AsyncRx.delay (100 * i)
            |> AsyncRx.requestAnimationFrame
            |> AsyncRx.map (fun m -> Letter (i, string c, int m.clientX + i * 10 + 15 - left, int m.clientY - top))
    } |> AsyncRx.tag "time"

printf "Starting program"

let app () = Reaction.StreamView initialModel view update stream
mountById "reaction-app" (ofFunction app () [])
