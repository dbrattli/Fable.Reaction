=========
Operators
=========

The following parameterized async observerable returning functions
(operators) are currently supported. Other operators may be implemented
on-demand, but the goal is to keep it simple and not make this into a
full featured `ReactiveX <http://reactivex.io/>`_ implementation (if
possible).

To use the operators open either the `Reaction` namespace.

.. code:: fsharp

    open Reaction

    xs = AsyncRx.single 42

You can also open the ``Reaction.AsyncRx`` module if you don't
want to prepend every operator with ``AsyncRx``. Be aware of
possible namespace conflicts with operators such as ``map``.

.. code:: fsharp

    open Reaction.AsyncRx

    xs = single 42

For the examples below we assume that ``Reaction.AsyncRx`` is opened.

Creating
========

Functions for creating (``'a -> IAsyncObservable<'a>``) an async observable.

.. val:: empty
    :type: : unit -> IAsyncObservable<'a>

    Returns an async observable sequence with no elements. You must usually
    indicate which type the resulting observable should be since empty
    itself doesn't produce any values.

    **Example:**

    .. code:: fsharp

        let xs = AsyncRx.empty<int> ()

.. val:: single
    :type: x:'a -> IAsyncObservable<'a>

    Returns an observable sequence containing the single specified
    element.

    **Example:**

    .. code:: fsharp

        let xs = AsyncRx.single 42

.. val:: fail
    :type: error:exn -> IAsyncObservable<'a>

    Returns the observable sequence that terminates exceptionally
    with the specified exception.

    **Example:**

    .. code:: fsharp

        exception MyError of string

        let error = MyError "error"
        let xs = AsyncRx.fail<int> error

.. val:: defer
    :type: factory:(unit -> IAsyncObservable<'a>) -> IAsyncObservable<'a>

    Returns an observable sequence that invokes the specified factory
    function whenever a new observer subscribes.

.. val:: create
    :type: subscribe:(IAsyncObserver<'a> -> Async<IAsyncDisposable>) -> IAsyncObservable<'a>

    Creates an async observable (`AsyncObservable<'a>`) from the
    given subscribe function.


.. val:: ofSeq
    :type: seq<'a> -> IAsyncObservable<'a>

    Returns the async observable sequence whose elements are pulled
    from the given enumerable sequence.

.. val:: ofAsyncSeq
    :type: AsyncSeq<'a> -> IAsyncObservable<'a>

    Convert async sequence into an async observable *(Not available in Fable)*.

.. val:: timer
    :type: int -> IAsyncObservable<int>

    Returns an observable sequence that triggers the value 0
    after the given duetime.

.. val:: interval
    :type: int -> IAsyncObservable<int>

    Returns an observable sequence that triggers the increasing
    sequence starting with 0 after the given period.

Transforming
============

Functions for transforming (``IAsyncObservable<'a> ->
IAsyncObservable<'b>``) an async observable.

.. val:: map
    :type: mapper:('a -> 'b) -> source: IAsyncObservable<'a> -> IAsyncObservable<'b>

    Returns an observable sequence whose elements are the result of invoking
    the mapper function on each element of the source.

    **Example:**

    .. code:: fsharp

        let mapper x = x * 10

        let xs = AsyncRx.single 42 |> AsyncRx.map mapper

.. val:: mapi
    :type: mapper:('a*int -> 'b) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Returns an observable sequence whose elements are the result of
    invoking the mapper function and incorporating the element's index
    on each element of the source.

.. val:: mapAsync
    :type: ('a -> Async<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Returns an observable sequence whose elements are the result of
    invoking the async mapper function on each element of the source.

.. val:: mapiAsync
    :type: ('a*int -> Async<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Returns an observable sequence whose elements are the result of
    invoking the async mapper function by incorporating the element's
    index on each element of the source.

.. val:: flatMap
    :type: ('a -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Projects each element of an observable sequence into an observable
    sequence and merges the resulting observable sequences back into one
    observable sequence.

.. val:: flatMapi
    :type: ('a*int -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Projects each element of an observable sequence into an observable
    sequence by incorporating the element's index on each element of the
    source. Merges the resulting observable sequences back into one
    observable sequence.

.. val:: flatMapAsync
    :type: ('a -> Async\<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Asynchronously projects each element of an observable sequence into
    an observable sequence and merges the resulting observable sequences
    back into one observable sequence.

.. val:: flatMapiAsync
    :type: ('a*int -> Async<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Asynchronously projects each element of an observable sequence into
    an observable sequence by incorporating the element's index on each
    element of the source. Merges the resulting observable sequences
    back into one observable sequence.

.. val:: flatMapLatest
    :type: ('a -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Transforms the items emitted by an source sequence into observable
    streams, and mirror those items emitted by the most-recently
    transformed observable sequence.

.. val:: flatMapLatestAsync
    :type: ('a -> Async<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Asynchronosly transforms the items emitted by an source sequence
    into observable streams, and mirror those items emitted by the
    most-recently transformed observable sequence.

.. val:: catch
    :type: (exn -> IAsyncObservable<'a>) -> IAsyncObservable<'a> -> IAsyncObservable<'a>

    Returns an observable sequence containing the first sequence's
    elements, followed by the elements of the handler sequence in case
    an exception occurred.

Filtering
=========

Functions for filtering (``IAsyncObservable<'a> ->
IAsyncObservable<'a>``) an async observable.

.. val:: filter
    :type: predicate:('a -> bool) -> IAsyncObservable<'a> -> IAsyncObservable<'a>

    Filters the elements of an observable sequence based on a
    predicate. Returns an observable sequence that contains elements
    from the input sequence that satisfy the condition.

    **Example:**

    .. code:: fsharp

        let predicate x = x < 3

        let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.filter predicate

.. val:: filterAsync
    :type:  ('a -> Async<bool>) -> IAsyncObservable<'a> -> IAsyncObservable<'a>

    Filters the elements of an observable sequence based on an async
    predicate. Returns an observable sequence that contains elements
    from the input sequence that satisfy the condition.

.. val:: distinctUntilChanged
    :type: IAsyncObservable<'a> -> IAsyncObservable<'a>

    Return an observable sequence only containing the distinct
    contiguous elementsfrom the source sequence.

.. val:: takeUntil
    :type: IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a>

    Returns the values from the source observable sequence until the
    other observable sequence produces a value.

.. val:: choose
    :type: ('a -> 'b option) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Applies the given function to each element of the stream and returns
    the stream comprised of the results for each element where the
    function returns Some with some value.

.. val:: chooseAsync
    :type: ('a -> Async<'b option>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

    Applies the given async function to each element of the stream and
    returns the stream comprised of the results for each element
    where the function returns Some with some value.


Aggregating
===========

.. val:: scan
    :type: initial:'s -> accumulator:('s -> 'a -> 's) -> source: IAsyncObservable<'a> -> IAsyncObservable<'s>

    Applies an accumulator function over an observable sequence for every
    value `'a` and returns each intermediate result `'s`. The `initial` seed
    value is used as the initial accumulator value. Returns an observable
    sequence containing the accumulated values `'s`.

    **Example:**

    .. code:: fsharp

        let scanner a x = a + x

        let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.scan 0 scanner

.. val:: scanAsync
    :type: initial: 's -> accumulator: ('s -> 'a -> Async<'s>) -> source: IAsyncObservable<'a> -> IAsyncObservable<'s>

    Applies an async accumulator function over an observable
    sequence and returns each intermediate result. The seed value is
    used as the initial accumulator value. Returns an observable
    sequence containing the accumulated values.

    **Example:**

    .. code:: fsharp

        let scannerAsync a x = async { return a + x }

        let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.scanAsync 0 scannerAsync

.. val:: groupBy
    :type: keyMapper: ('a -> 'g) -> source: IAsyncObservable<'a> -> IAsyncObservable<IAsyncObservable<'a>>

    Groups the elements of an observable sequence according to a
    specified key mapper function. Returns a sequence of observable
    groups, each of which corresponds to a given key.

    **Example:**

    .. code:: fsharp

        let xs = AsyncRx.ofSeq [1; 2; 3; 4; 5; 6]
            |> AsyncRx.groupBy (fun x -> x % 2)
            |> AsyncRx.flatMap (fun x -> x)

Combining
=========

Functions for combining multiple async observables into one.

- **merge** : IAsyncObservable<'a> -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **mergeInner** : IAsyncObservable\<IAsyncObservable<'a>\> -> IAsyncObservable<'a>
- **switchLatest** : IAsyncObservable<IAsyncObservable<'a>> -> IAsyncObservable<'a>
- **concat** : seq<IAsyncObservable<'a>> -> IAsyncObservable<'a>
- **startWith** : seq<'a> -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **combineLatest** : IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>
- **withLatestFrom** : IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>
- **zipSeq** : seq<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>

Time-shifting
=============

Functions for time-shifting (``IAsyncObservable<'a> ->
IAsyncObservable<'a>``) an async observable.

- **delay** : int -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **debounce** : int -> IAsyncObservable<'a> -> IAsyncObservable<'a>

.. val:: sample
    :type: msecs: int -> source: IAsyncObservable<'a> -> IAsyncObservable<'a>

    Samples the observable sequence at each interval.

Leaving
=======

Functions for leaving (``IAsyncObservable<'a> -> 'a``) the async observable.

.. val:: toAsyncSeq
    :type: IAsyncObservable<'a> -> AsyncSeq<'a>

    *(Not available in Fable)*
