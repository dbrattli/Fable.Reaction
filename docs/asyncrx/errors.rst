==============
Error Handling
==============

Reactive applications also need to handle exceptions. But unlike normal
applications where exceptions fly upwards, exceptions in a reactive
application goes the other way and propagates down the chain of
operators.

Observables will dispose the subscription if any errors occur. No event
can be observed after an `OnError`. This may sound like a restriction,
but if we think of the dual world of iterables, then we know that if the
iterator throws an exception, then the iteration is over. Observables
works in just the same but opposite way.

When dealing with errors there are several strategies to choose:

- Retrying subscriptions
- Catching exceptions
- Ignoring exceptions
- Logging errors

It's important to notice that observer implementations should never
throw exceptions. This is because we expect that (1) the observer
implementor knows best how to handle those exceptions and we can't do
anything reasonable with them, and (2) if an exception occurs then we
want that to bubble out of and not into the reactive framework.

The following error handling operators have been implemented:

.. val:: catch
    :type: (exn -> IAsyncObservable<'a>) -> IAsyncObservable<'a> -> IAsyncObservable<'a>

    Returns an observable sequence containing the first sequence's
    elements, followed by the elements of the handler sequence in case
    an exception occurred.
