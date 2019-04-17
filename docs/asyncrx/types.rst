Types
=====

Reaction is built around the interfaces ``IAsyncDisposable``,
``IAsyncObserver`` and ``IAsyncObservable``. This enables a familiar Rx
programming syntax similar to `FSharp.Control.Reactive
<http://fsprojects.github.io/FSharp.Control.Reactive/reference/fsharp-control-reactive-observablemodule.html>`_
with the difference that all methods are `Async`.

.. code:: fsharp

    type IAsyncDisposable =
        abstract member DisposeAsync: unit -> Async<unit>

    type IAsyncObserver<'a> =
        abstract member OnNextAsync: 'a -> Async<unit>
        abstract member OnErrorAsync: exn -> Async<unit>
        abstract member OnCompletedAsync: unit -> Async<unit>

    type IAsyncObservable<'a> =
        abstract member SubscribeAsync: IAsyncObserver<'a> -> Async<IAsyncDisposable>

The relationship between these three interfaces can be seen in this
single line of code. You get an async disposable by subscribing
asynchronously to an async observable with an async observer:

.. code:: fsharp

    async {
        let! disposableAsync = observable.SubscribeAsync observerAsync
        ...
    }

Notifications
=============

Observers may written as a function accepting ``Notification<'a>`` which
is defined as the following discriminated union:

.. code:: fsharp

    type Notification<'a> =
        | OnNext of 'a
        | OnError of exn
        | OnCompleted

You can read more about observers in the [Observers](./Observers.md)
section.