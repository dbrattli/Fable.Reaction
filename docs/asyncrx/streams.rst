=======
Streams
=======

You can think of a "Stream" as being a tube that is open at both sides. Streams
combines both the ``IAsyncObservable<'a>`` and ``IAsyncObserver<'a>``
interfaces. Whatever you put in (e.g ``OnNextAsync``) at one side
(``IAsyncObserver<'a>``) will come out of the other end
(``IAsyncObservable<'a>``).

Streams are similar to the classic `Subject` in `ReactiveX
<http://reactivex.io/>`_. The difference is that a Stream in Reaction is not a
single object, but a tuple of two *entangled* objects where one implements
``IAsyncObserver<'a>``, and the other implements ``IAsyncObservable<'a>``. This
solves the problem of having a single object trying to be two things at once
(what is the dual of an ``ISubject<'a>`` anyways?).

There are currently 3 types of streams in Reaction, **stream**,
**mbStream** and  **singleStream**.

.. val:: stream
    :type: unit -> IAsyncObserver<'a> * IAsyncObservable<'a>

    The stream will forward any notification pushed to the
    ``IAsyncObserver<'a>`` side to all observers that have subscribed to the
    ``IAsyncObservable<'a>`` side. The stream is hot in the sense that if
    there are no observers, then the notification will be lost.

    .. code:: fsharp

        let dispatch, obs = AsyncRx.stream ()

        let main = async {
            let! sub = obs.SubscribeAsync obv
            do! dispatch.OnNextAsync 42
            ...
        }

        Async.StartImmediate main ()

.. val:: mbStream
    :type: unit -> MailboxProcessor<Notification<'a>> * IAsyncObservable<'a>

    The Mailbox Stream is the same as a ``stream`` except that the observer is
    exposed as a ``MailboxProcessor<Notification<'a>>``. The mailbox stream is
    hot in the sense that if there are no observers, then any pushed
    notification will be lost.

    .. code:: fsharp

        let dispatch, obs = AsyncRx.mbStream ()

        let main = async {
            let! sub = obs.SubscribeAsync obv
            do dispatch.Post (OnNext 42)
            ...
        }

        Async.StartImmediate main ()

.. val:: singleStream
    :type: unit -> IAsyncObserver<'a> * IAsyncObservable<'a>

    The single stream will forward any notification pushed to the
    ``IAsyncObserver<'a>`` side to a single observer that have subscribed to
    the ``IAsyncObservable<'a>`` side. The single stream is "cold" in the sense
    that if there's no-one observing, then the writer will be awaited until
    there is a subscriber that can observe the value being pushed. You can use
    a single stream in scenarios that requires so called backpressure.

    .. code:: fsharp

        let dispatch, obs = AsyncRx.singleStream ()

        let main = async {
            let! sub = obs.SubscribeAsync obv
            do! dispatch.OnNextAsync 42
            ...
        }

        Async.StartImmediate main ()
