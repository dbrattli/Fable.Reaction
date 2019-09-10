=======
Streams
=======

A stream in Elmish Streams makes it easy to compose stream components into the
message flow. A stream is an abstraction for a collection of named Async
Observables (AsyncRx). The main purpose is to to make it easier to subscribe
and dispose multiple observables from different components and sub-components
in your application.

By naming the observables we can subscribe or dispose them individually at
runtime based on the current model. If the model changes, subscriptions
changes. This effectivly solves the lifetime management of event subscriptions,
e.g when switching pages in a single page application.

A stream also implements ``IAsyncObservable<'msg>`` so you may use all the
AsyncRx functions to transform your stream. To convert an Async Observable back
into a stream you need to name it using ``AsyncRx.toStream`` (see below).

.. type:: Subscription<'msg,'name>
    :manifest: IAsyncObservable<'msg>*'name

    A Subscription is a named Async Observable, i.e an Async Observable ready
    to be subscribed. This enable us to subscribe and dispose subscriptions
    individually.

.. type:: Stream<'msg,'name>
    :kind: Stream of Subscription<'msg, 'name> list

    A stream is a container of subscriptions.

This is extremely powerful since you can change the behaviour of your stream
whenever your model is updated. The stream will transform any message before it
hits the update function.

Below is a number of helper functions for working on streams.

.. module:: Stream

    Stream extension methods

    .. val:: none
        :type: Stream<'msg, 'name>

        None - no stream. You can use this to dispose a previously subscribed stream.

    .. val:: map
        :type: (f: 'a -> 'msg) -> Stream<'a, 'name> -> Stream<'msg, 'name>

        Map stream from one message type to another.

    .. val:: filter
        :type: filter (predicate: 'msg -> bool) -> Stream<'msg, 'name> -> Stream<'msg, 'name>

        Filter stream based on given predicate.

    .. val:: batch
        :type: (streams: #seq<Stream<'msg, 'name>>) -> Stream<'msg, 'name>

        Aggregate multiple streams

    .. val:: tap
        :type: tag:string -> Stream<'msg, 'name> -> Stream<'msg, 'name>

        Tap into stream and print messages to console. The tag is a string used
        to give yourself a hint of where the tap is inserted. Returns the
        stream unmodified.

    .. val:: choose
        :type: (chooser: 'a -> 'msg option) -> Stream<'a, 'name> -> Stream<'msg, 'name>

        Applies the given chooser function to each element of the stream and
        returns the stream comprised of the results for each element where the
        function returns with Some value.

        .. code:: fsharp

            let asMagicMsg = function
                | MagicMsg msg -> Some msg
                | _ -> None

            msgs |> Stream.choose asMagicMsg

    .. val:: chooseNot
        :type: (chooser: 'a -> 'msg option) -> Stream<'a, 'name> -> Stream<'msg, 'name>

        Applies the given chooser function to each element of the stream and
        returns the stream comprised of the results for each element where the
        function returns with None value.

        .. code:: fsharp

            let asMagicMsg = function
                | MagicMsg msg -> Some msg
                | _ -> None

            msgs |> Stream.chooseNot asMagicMsg

        The ``chooseNot`` function is just a convenience for writing:

        .. code::

            filter (chooser >> Option.isNone)

    .. val:: chooseNamed
        :type: name:'name -> (chooser:'a -> 'msg option) -> Stream<'a, 'name> -> Stream<'msg, 'name>

        Selects the stream with the given name and applies the given chooser
        function to each element of the stream and returns the stream comprised
        of the results for each element where the function returns with Some
        value.

    .. val:: subStream
        :type: (stream :'subModel -> Stream<'subMsg,'name> -> Stream<'subMsg,'name>) ->
            (model:'subModel) ->
            (toSubMsg:'msg -> 'subMsg option) ->
            (toMsg:'subMsg -> 'msg) ->
            (name:'name) ->
            (msgs:Stream<'msg,'name>)
                -> Stream<'msg,'name>

        Composes a sub-stream of a sub-component into the main component. The
        sub-stream messages are removed from the message stream and sent
        through the stream function of the sub-component. The sub-meesages that
        flows out of the sub-component is then merged back into the main
        message stream. This makes stream components symmetric in the sense
        that streams of the main application is written exactly the same way as
        the streams of the components and sub-components.

        .. code:: fsharp

            let asMagicMsg = function
                | MagicMsg msg -> Some msg
                | _ -> None

            msgs
            |> Stream.subStream Magic.stream model.Magic asMagicMsg MagicMsg "magic"

        The ``subStream`` function is is just a convenience for writing:

        .. code:: fsharp

            let subMsgs = Stream [ msgs |> AsyncRx.choose toSubMsg, name]
                let subMsgs' = stream subModel subMsgs |> map toMsg
                let msgs' = msgs |> chooseNot toSubMsg

                batch [
                    subMsgs'
                    msgs'
                ]
