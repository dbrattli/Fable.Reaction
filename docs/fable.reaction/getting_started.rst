===============
Getting Started
===============

To use Fable.Reaction you need to create an instance of a `streamComponent`.
The `streamComponent` works almost the same way as an Elmish application
taking an initial model, a view and and update function. In addition it
takes a stream function that transforms the stream of dispatched
messages before they reach the update function.

.. code:: fsharp

    open FSharp.Control.AsyncRx // 1. Enable AsyncRx
    open Fable.Reaction // 2. Enable Fable Reaction

    let update (currentModel : Model) (msg : Msg) : Model =
        ...

    let view (model : Model) (dispatch : (Msg -> unit)) =
        ...

    let stream model msgs = // 3. Add the reactive stream
        msgs
        |> AsyncRx.delay 1000
        |> AsyncRx.toStream "msgs"

    let app = Reaction.streamComponent(initialModel, view, update, stream)
    mountById "reaction-app" (ofFunction app () [])

Loading initial State
=====================

To load initial state from the server without using commands (`Cmd`) you
create an Async Observable using `ofPromise` and then concat the result
into the message stream. Thus the message stream in the example below
will start with the initialCountLoaded message.

.. code:: fsharp

    // Add open statements to top of file
    open Fable.Reaction

    let loadCount =
        ofPromise (fetchAs<int> "/api/init" [])
            |> AsyncRx.map (Ok >> InitialCountLoaded)
            |> AsyncRx.catch (Error >> InitialCountLoaded >> single)
            |> AsyncRx.toStream "loading"

    let stream model msgs =
        match model with
        | Loading ->
            loadCount
        | _ ->
            msgs

Using Fable.Reaction with Elmish
================================

Fable.Reaction can be used all by itself without Elmish. But if you want
to use Fable Reaction with Elmish you just add the Fable.Reaction component
to the Elmish view like any other element such as e.g `div` and `str`. A
Fable.Reaction component produces a `ReactElement` that can be used
anywhere in your view such as with the `autocomplete` component below.

.. code:: fsharp

    let view (model: Model) (dispatch : Dispatch<Msg>) =
        Container.container [] [
            h1 [] [
                str "Search Wikipedia"
            ]
            autocomplete { Search=searchWikipedia; Dispatch = Select >> dispatch; DebounceTimeout=750 }

            div [ Style [ MarginTop "30px" ]] [
                match model.Selection with
                | Some selection ->
                    yield str "Selection: "
                    yield str selection
                | None -> ()
            ]
        ]



Doing side effects per message
==============================

In the example below we flat map (map and merge) the result of querying
Wikipedia back into the message stream. The ``flatMapLatest`` operator
is a combination of the ``map`` and ``switchLatest`` operators. This
operator works like ``flatMap`` but will auto-cancel any ongoing fetch
operation if a new query is made before the previous result is ready.

.. code:: fsharp

    // Add open statements to top of file
    open Fable.Reaction

    let stream model msgs =
        msgs
        |> AsyncRx.choose Msg.asKeyboardEvent
        |> AsyncRx.map targetValue
        |> AsyncRx.filter (fun term -> term.Length > 2 || term.Length = 0)
        |> AsyncRx.debounce 750          // Pause for 750ms
        |> AsyncRx.distinctUntilChanged  // Only if the value has changed
        |> AsyncRx.flatMapLatest searchWikipedia
        |> AsyncRx.toStream "msgs"

