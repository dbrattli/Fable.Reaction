module Client

open Fable.React
open Fable.React.Props
open Fable.Reaction
open FSharp.Control
open Fulma
open Browser.Types
open Fable.Core.JsInterop

type Project = { Name: string; Logo: string }

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type TopLeft = { Top: float; Left: float }
type Model = Map<Project, TopLeft>

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    | MouseDownEvent of Browser.Types.MouseEvent * Project
    | MouseUpEvent of Browser.Types.MouseEvent * Project
    | MouseDragEvent of float * float * Project

    static member asMouseDownEvent = function
        | MouseDownEvent (ev, logo) -> Some (ev, logo)
        | _ -> None

    static member asMouseUpEvent = function
        | MouseUpEvent (ev, logo) -> Some (ev, logo)
        | _ -> None


// defines the initial state and initial command (= side-effect) of the application
let init () : Model =
    [
        { Name = "Reaction"; Logo= "url(Images/logo-reaction.png)" }, { Top = 150.0; Left = 100.0 }
        { Name = "ReactiveX"; Logo = "url(Images/logo.png)" } , { Top = 200.0; Left = 700.0 }
        { Name = "Fable"; Logo = "url(Images/logo-fable.png)" } , { Top = 300.0; Left = 400.0 }
    ]
    |> Map.ofSeq

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (currentModel : Model) (msg : Msg) : Model  =
    match msg with
    | MouseDragEvent (left, top, logo) ->
        currentModel.Add (logo, { Top=top; Left=left })
    | _ ->
        currentModel

let safeComponents =
    let components =
        span [ ]
           [
             a [ Href "https://github.com/giraffe-fsharp/Giraffe" ] [ str "Giraffe" ]
             str ", "
             a [ Href "http://fable.io" ] [ str "Fable" ]
             str ", "
             a [ Href "https://github.com/dbrattli/Fable.Reaction" ] [ str "Reaction" ]
             str ", "
             a [ Href "https://mangelmaxime.github.io/Fulma" ] [ str "Fulma" ]
           ]

    p [ ] [
        strong [] [ str "Powered by " ]
        components ]

let view (model : Model) (dispatch : Msg -> unit) =
    let renderLogos model =
        div [] [
            for KeyValue(project, pos) in model do
                yield
                    div [ OnMouseUp (fun ev -> MouseUpEvent (ev, project) |> dispatch)
                          OnMouseDown (fun ev -> MouseDownEvent (ev, project) |> dispatch)
                          Style [ Top pos.Top; Left pos.Left; Height 200; Width 200; Padding 10;
                                  Position PositionOptions.Absolute; Cursor "move"; Border 1; BorderStyle "solid"
                                  BackgroundColor "#000000"; Color "#ffffff"; BackgroundImage project.Logo
                                  BackgroundPosition "center"; BackgroundRepeat "no-repeat"
                                  BackgroundSize "200px 200px"]] [
                        str project.Name
                    ]
        ]

    div [] [
        Navbar.navbar [ Navbar.Color IsPrimary ] [
            Navbar.Item.div [] [
                Heading.h2 [] [
                    str "Fable Reaction Drag'n Drop"
                ]
            ]
        ]

        renderLogos model

        Footer.footer [] [
            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] [
                safeComponents
            ]
        ]
    ]

let stream model msgs =
    let mouseMove = AsyncRx.ofMouseMove ()
    let mouseUp =
        msgs
        |> AsyncRx.choose Msg.asMouseUpEvent

    let mouseDown =
        msgs
        |> AsyncRx.choose Msg.asMouseDownEvent

    (*
    mouseDown
    |> flatMap (fun ev ->
        let rect = ev.nativeEvent.srcElement.getBoundingClientRect ()
        let startX, startY = (ev.clientX - rect.left, ev.clientY - rect.top)

        mouseMove
        |> AsyncRx.map (fun ev ->
            MouseDragEvent ev.clientX - startX, ev.clientY - startY)
        |> AsyncRx.takeUntil mouseUp)
    *)
    asyncRx {
        let! ev, project = mouseDown
        let rect : ClientRect = !!ev.target?getBoundingClientRect ()
        let startX, startY = ev.clientX - rect.left, ev.clientY - rect.top

        yield! mouseMove
            |> AsyncRx.map (fun ev ->
                MouseDragEvent (ev.clientX - startX, ev.clientY - startY, project))
            |> AsyncRx.takeUntil mouseUp
    } |> AsyncRx.tag "dnd"


let initialModel = init ()
let app = Reaction.StreamView initialModel view update stream
mountById "reaction-app" (ofFunction app () [])
