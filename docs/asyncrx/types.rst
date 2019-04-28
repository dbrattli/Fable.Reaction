=====
Types
=====

FSharp.Control.AsyncRx is built around the interfaces ``IAsyncDisposable``,
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

Async Observables
=================

AsyncRx is an implementation of Async Observable. The difference between an
"Async Observable" and an "Observable" is that with "Async Observables" you
need to await methods such as ``Subscribe``, ``OnNext``, ``OnError``, and
``OnCompleted``. In AsyncRx they are thus called ``SubscribeAsync``,
``OnNextAsync``, ``OnErrorAsync``, and ``OnCompletedAsync``. This enables
``SubscribeAsync`` to await async operations i.e setup network connections, and
observers (``OnNext``) may finally await side effects such as writing to disk
(observers are all about side-effects right?).

This diagram shows the how Async Observables relates to other
collections and values.

+-------------------+----------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------------------------+
|                   |                                                     Single Value                                                     |                                                              Multiple Values                                                              |
+===================+======================================================================================================================+===========================================================================================================================================+
| Synchronous pull  | unit -> 'a                                                                                                           | `seq<'a> <https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/collections.seq-module-%5Bfsharp%5D?f=255&MSPPError=-2147217396>`_ |
+-------------------+----------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------------------------+
| Synchronous push  | 'a -> unit                                                                                                           | `Observable<'a> <http://fsprojects.github.io/FSharp.Control.Reactive/tutorial.html>`_                                                     |
+-------------------+----------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------------------------+
| Asynchronous pull | unit -> `Async<'a> <https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/control.async-class-%5Bfsharp%5D>`_ | `AsyncSeq<'a> <http://fsprojects.github.io/FSharp.Control.AsyncSeq/library/AsyncSeq.html>`_                                               |
+-------------------+----------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------------------------+
| Asynchronous push | 'a -> `Async<unit> <https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/control.async-class-%5Bfsharp%5D>`_ | **AsyncObservable<'a>**                                                                                                                   |
+-------------------+----------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------------------------------------------------------------------------------------------+

Async Observers
===============

Observers (``IAsyncObserver<'a>``) may be subscribed to observables in
order to receive notifications. An observer is defined as an
implementation of ``IAsyncObserver<'a>``:

.. code:: fsharp

    let _obv =
        { new IAsyncObserver<'a> with
            member this.OnNextAsync x = async {
                printfn "OnNext: %d" x
            }
            member this.OnErrorAsync err = async {
                printfn "OnError: %s" (ex.ToString())
            }
            member this.OnCompletedAsync () = async {
                printfn "OnCompleted"
            }
        }

There is also a ``SubscribeAsync`` overload that takes a notification
function so instead of using ``IAsyncObserver<'a>`` instances you may
subscribe with a single async function taking a ``Notification<'a>``.

.. code:: fsharp

    let obv n =
        async {
            match n with
            | OnNext x -> printfn "OnNext: %d" x
            | OnError ex -> printfn "OnError: %s" (ex.ToString())
            | OnCompleted -> printfn "OnCompleted"
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

