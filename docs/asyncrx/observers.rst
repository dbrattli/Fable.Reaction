Observers
=========

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
