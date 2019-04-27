=======
Streams
=======

.. type:: Subscription<'msg,'name>
    :manifest: IAsyncObservable<'msg>*'name

    A named Async Observable to be subscribed.

.. type:: Stream<'msg,'name>
    :kind: Stream of Subscription<'msg, 'name> list

    Container for subscriptions that may produce messages

.. module:: Stream

    Stream extension methods

    .. val:: none
        :type: Stream<'msg, 'name>

        None - no stream. Use to dispose a previously subscribed stream.

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

        Tap into stream and print messages to console. The tag is a
        string used to give yourself a hint of where the tap is
        inserted. Returns the stream unmodified.

    .. val:: choose
        :type: (chooser: 'a -> 'msg option) -> Stream<'a, 'name> -> Stream<'msg, 'name>

        Applies the given chooser function to each element of the stream
        and returns the stream comprised of the results for each element
        where the function returns with Some value.

    .. val:: chooseNot
        :type: (chooser: 'a -> 'msg option) -> Stream<'a, 'name> -> Stream<'msg, 'name>

        Applies the given chooser function to each element of the stream and
        returns the stream comprised of the results for each element where the
        function returns with None value.

    .. val:: chooseNamed
        :type: name:'name -> (chooser:'a -> 'msg option) -> Stream<'a, 'name> -> Stream<'msg, 'name>

        Selects the stream with the given name and applies the given
        chooser function to each element of the stream and returns the
        stream comprised of the results for each element where the
        function returns with Some value.

