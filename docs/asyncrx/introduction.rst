============
Introduction
============

FSharp.Control.AsyncRx is a library for asynchronous and reactive programming, and is
an implementation of Async Observables (`ReactiveX
<http://reactivex.io/>`_) for F# and `Fable <http://fable.io/>`_.
FSharp.Control.AsyncRx makes it easy to compose and combine streams of asynchronous
event based data such as timers, mouse-moves, keyboard input, web
requests and enables you to do operations such as:

- Filter
- Transform
- Aggregate
- Combine
- Time-shift

FSharp.Control.AsyncRx was designed spesifically for targeting `Fable
<http://fable.io/>`_ which means that the code may be `transpiled
<https://en.wikipedia.org/wiki/Source-to-source_compiler>`_ to
JavaScript, and thus the same F# code may be used both client and server
side for full stack software development.

See :doc:`/fable.elmish/index`  for
use of FSharp.Control.AsyncRx with Elmish and the model view update (MVU) style
architecture.
