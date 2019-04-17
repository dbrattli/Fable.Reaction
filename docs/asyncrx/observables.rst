Async Observables
=================

Reaction is an implementation of Async Observable. The difference
between an "Async Observable" and an "Observable" is that with "Async
Observables" you need to await methods such as ``Subscribe``,
``OnNext``, ``OnError``, and ``OnCompleted``. In Reaction they are thus
called ``SubscribeAsync``, ``OnNextAsync``, ``OnErrorAsync``, and
``OnCompletedAsync``. This enables ``SubscribeAsync`` to await async
operations i.e setup network connections, and observers (``OnNext``) may
finally await side effects such as writing to disk (observers are all
about side-effects right?).

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
