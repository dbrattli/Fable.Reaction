Creating Custom Observables
===========================

There are many ways to create custom observble streams. Note that the goal
is to have some kind of create function that returns an ``IAsyncObservable<'a>``.
We will go through a few options for creating such a custom stream.

1. Use a Stream
---------------

A Stream in Reaction return both an observer (``IAsyncObserver``) and an
observable (``IAsyncObservable``). Note that we need to use
``Async.Start`` to start the worker function so it runs concurrently.

.. code:: fsharp

    open Reaction

    let myStream () =
        let dispatch, obs = AsyncRx.stream<Msg> ()

        let worker () = async {
            while true do
                let! msg = getMessageAsync ()
                do! dispatch.OnNextAsync msg
        }

        Async.Start (worker ())
        obs

2. Use Create
-------------

The ``AsyncRx.Create`` function takes an ``Async`` subscribe function and
returns an ``IAsyncObservable``. Note that we need to use `Async.Start` to start
the worker function so it runs concurrently.

.. code:: fsharp

    open Reaction

    let myStream () =
        let subscribeAsync (obs: IAsyncObserver<Msg>) : Async<IAsyncDisposable> =
            let mutable running = true

            async {
                let worker () = async {
                    while running do
                        let! msg = getMessageAsync ()
                        do! obs.OnNextAsync msg
                    }

                Async.Start (worker ())

                let cancel () = async {
                    running <- false
                }

                return AsyncDisposable.Create(cancel)
            }

        AsyncRx.create(subscribeAsync)

3. Use ofAsyncWorker
--------------------

The ``ofAsyncWorker`` is a handy utility function for creating an
``IAsyncObservable`` from an async worker function, where the worker
function has the type ``IAsyncObserver<'a> -> CancellationToken ->
Async<unit>``. Thus the worker will receive a cancellation token that
can be used to detect if cancellation (dispose) have been requested.

.. code:: fsharp

    open Reaction
    open System.Threading

    let myStream' () =
        let worker (obv: IAsyncObserver<Msg>) (token: CancellationToken)  = async {
            while not token.IsCancellationRequested do
                let! msg = getMessageAsync ()
                do! obv.OnNextAsync msg
        }

        Create.ofAsyncWorker(worker)
