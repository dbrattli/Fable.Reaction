====
Demo
====

Time Flies Like an Arrow
========================

.. raw:: html

    <div id="elmish-app"></div>

    <script src="../_static/bundle.js"></script>

The Timeflies example (`source code
<https://github.com/dbrattli/Reaction/blob/master/examples/Timeflies/src/Client/Client.fs>`_)
implements the classic `Time
Flies <https://blogs.msdn.microsoft.com/jeffva/2010/03/17/reactive-extensions-for-javascript-the-time-flies-like-an-arrow-sample/>`_
example from `RxJS <https://rxjs-dev.firebaseapp.com/>`_.

In the Time files demo the stream of mouse moves are transformed into a
stream of letters where each letter is delayed according to its
position.

The ``Model`` type holds data that you want to keep track of while the
application is running.

.. code:: fsharp

    type Model = {
        Letters: Map<int, string * int * int>
    }

The ``Msg`` type defines what events/actions can occur while the
application is running. The state of the application changes *only* in
reaction to these events

.. code:: fsharp

    type Msg =
        | Letter of int * string * int * int

The update function computes the next state of the application based on
the current state and the incoming messages

.. code:: fsharp

    let update (msg : Msg) (currentModel : Model) : Model =
        match currentModel.Letters, msg with
        | _, Letter (i, c, x, y) ->
            { currentModel with Letters = currentModel.Letters.Add (i, (c, x, y)) }


The view function renders the model and the result will be handled over
to React. It produces a div element and then takes each letter in the
Map and adds a span element containing the letter. The span element will
have the top/left properties set so that the letter is rendered at the
correct position on the page.

.. code:: fsharp

    let view (model : Model) (dispatch : Dispatch<Msg>) =
        let letters = model.Letters

        div [ Style [ FontFamily "Consolas, monospace"; Height "100%"] ]
            [
                [ for KeyValue(i, (c, x, y)) in letters do
                    yield span [ Key (c + string i); Style [Top y; Left x; Position "fixed"] ]
                        [ str c ] ] |> ofList
            ]

The init funciton produces an empty model.

.. code:: fsharp

    let init () : Model =
        { Letters = Map.empty }


Helper code to render the letters at the correct position on the page.

.. code:: fsharp

    let getOffset (element: Browser.Element) =
        let doc = element.ownerDocument
        let docElem = doc.documentElement
        let clientTop  = docElem.clientTop
        let clientLeft = docElem.clientLeft
        let scrollTop  = Browser.window.pageYOffset
        let scrollLeft = Browser.window.pageXOffset

        int (scrollTop - clientTop), int (scrollLeft - clientLeft)

    let container = Browser.document.querySelector "#elmish-app"
    let top, left = getOffset container


Message stream (expression style) that transforms the stream of mouse moves
into a stream of letters where each letter is delayed according to its position
in the stream.

.. code:: fsharp

    let stream (model : Model) (msgs:  Stream<Msg, string>) =
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
    |> Program.withStream stream "msgs"
    |> Program.withReactBatched "elmish-app"
    |> Program.run
