=======
Scaling
=======

Elmish Streams applications have a stream function that takes the current Model
(``'model``) as the first argument like this:

.. code:: fsharp

    val withStream:
        stream     : 'model -> Stream<'msg,'name> -> Stream<'msg,'name> ->
        initialName: 'name (requires comparison )->
        program    : Program<'a,'model,'msg,'b>
                    -> Program<'a,'model,'msg,'b>

And there is a helper for calling sub-queries for pages and componets that will
help you with (un)wrapping to and from (sub-)messages.

.. code:: fsharp

    val subStream:
        stream  : 'model -> Stream<'subMsg,'name> -> Stream<'subMsg,'name> ->
        model   : 'model             ->
        toMsg   : 'subMsg -> 'msg    ->
        toSubMsg: 'msg -> 'subMsg option ->
        name    : 'name              ->
        msgs    : Stream<'msg,'name>
        -> Stream<'msg,'name>

Thus a sub-stream can be called like this:

.. code:: fsharp

    let stream (model: Model) (msgs: Stream<Msg, string>) =
        match model.PageModel with
        | HomePageModel ->
            msgs
        ...
        | TomatoModel m ->
            msgs |>
            Stream.withSubStream Tomato.stream m TomatoMsg Msg.asTomatoMsg "tomato"

The ``Msg.asTomatoMsg`` is a helper function you can declare as an
extension on Msg (``'msg -> 'submsg option``). It takes a stream of
messages and returns a stream of sub-messages e.g:

.. code:: fsharp

    type Msg =
        ...
        | TomatoMsg of Tomato.Msg

        static member asTomatoMsg = function
            | TomatoMsg tmsg -> Some tmsg
            | _ -> None

