========
Subjects
========

You can think of a "Subject" as being a tube that is open at both sides.
Whatever you put in on one side, pops out at the other end. Subjects combines
both the ``IAsyncObservable<'a>`` and ``IAsyncObserver<'a>`` interfaces.
Whatever you put in (e.g ``OnNextAsync``) at one side (``IAsyncObserver<'a>``)
will come out of the other end (``IAsyncObservable<'a>``).

The subject in FSharp.AsyncRx is very similar to the classic `Subject` in
`ReactiveX <http://reactivex.io/>`_. The difference is that a is not a single
object, but a tuple of two *entangled* objects where one implements
``IAsyncObserver<'a>``, and the other implements ``IAsyncObservable<'a>``. This
solves the problem of having a single object trying to be two things at once
(what is the dual of an ``ISubject<'a>`` anyways?).

There are currently 3 types of subjects in AsyncRx, **subject**,
**mbSubject** and  **singleSubject**.

.. val:: subject
    :type: unit -> IAsyncObserver<'a> * IAsyncObservable<'a>

    The subject will forward any notification pushed to the
    ``IAsyncObserver<'a>`` side to all observers that have subscribed to
    the ``IAsyncObservable<'a>`` side. The subject is hot in the sense
    that if there are no observers, then the notification will be lost.

    .. code:: fsharp

        let dispatch, obs = AsyncRx.subject ()

        let main = async {
            let! sub = obs.SubscribeAsync obv
            do! dispatch.OnNextAsync 42
            ...
        }

        Async.StartImmediate main ()

.. val:: mbSubject
    :type: unit -> MailboxProcessor<Notification<'a>> * IAsyncObservable<'a>

    The Mailbox Subject is the same as a ``subject`` except that the observer
    is exposed as a ``MailboxProcessor<Notification<'a>>``. The mailbox subject
    is hot in the sense that if there are no observers, then any pushed
    notification will be lost.

    .. code:: fsharp

        let dispatch, obs = AsyncRx.mbSubject ()

        let main = async {
            let! sub = obs.SubscribeAsync obv
            do dispatch.Post (OnNext 42)
            ...
        }

        Async.StartImmediate main ()

.. val:: singleSubject
    :type: unit -> IAsyncObserver<'a> * IAsyncObservable<'a>

    The singleSubject will forward any notification pushed to the
    ``IAsyncObserver<'a>`` side to a single observer that have subscribed to
    the ``IAsyncObservable<'a>`` side. The singleSubject is "cold" in the sense
    that if there's no-one observing, then the writer will be awaited until
    there is a subscriber that can observe the value being pushed. You can use
    a singleSubject in scenarios that requires so called backpressure.

    .. code:: fsharp

        let dispatch, obs = AsyncRx.singleSubject ()

        let main = async {
            let! sub = obs.SubscribeAsync obv
            do! dispatch.OnNextAsync 42
            ...
        }

        Async.StartImmediate main ()
