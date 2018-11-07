# Operators

The following parameterized async observerable returning functions (operators) are
currently supported. Other operators may be implemented on-demand, but the goal is to keep it simple and not make this into a full featured [ReactiveX](http://reactivex.io/) implementation (if possible).

To use the operators open either the `Reaction` namespace.

```fs
open Reaction

xs = AsyncObserable.single 42
```

You can also open the `Reaction.AsyncObserable` module if you don't want to prepend every operator with `AsyncObservable`. Be aware of possible namespace conflicts with operators such as `map`.

```fs
open Reaction.AsyncObserable

xs = single 42
```

For the examples below we assume that `Reaction.AsyncObservale` is opened.

## Creating

Functions for creating (`'a -> IAsyncObservable<'a>`) an async observable.

### empty

Returns an async observable sequence with no elements. You must usually indicate which type the resulting observable should be since empty itself doesn't produce any values.

```fs
val empty : unit : IAsyncObservable<'a>
```

**Example:**

```fs
let xs = empty<int> ()
```

### single

Returns an observable sequence containing the single specified
element.

```fs
val single : x: 'a -> IAsyncObservable<'a> =
```

**Example:**

```fs
let xs = single 42
```

### fail

Returns the observable sequence that terminates exceptionally
with the specified exception.

```fs
val fail error: exn -> IAsyncObservable<'a> =
```

**Example:**

```fs
exception MyError of string

let error = MyError "error"
let xs = fail<int> error
```

### defer

Returns an observable sequence that invokes the specified factory
function whenever a new observer subscribes.

```fs
val defer : factory: (unit -> IAsyncObservable<'a>) -> IAsyncObservable<'a>
```

### create

Creates an async observable (`AsyncObservable<'a>`) from the
given subscribe function.

```fs
val create : subscribe: (IAsyncObserver<'a> -> Async<IAsyncDisposable>) -> IAsyncObservable<'a>
```

**Example:**

```fs
exception MyError of string

let error = MyError "error"
let xs = fail error
```

- **ofSeq** : `seq<'a> -> IAsyncObservable<'a>`, Returns the async observable sequence whose elements are pulled
    from the given enumerable sequence.
- **ofAsyncSeq** : `AsyncSeq<'a> -> IAsyncObservable<'a>`, Convert async sequence into an async observable *(Not available in Fable)*.
- **timer** : `int -> IAsyncObservable<int>`, Returns an observable sequence that triggers the value 0
    after the given duetime.
- **interval** `int -> IAsyncObservable<int>`, Returns an observable sequence that triggers the increasing
    sequence starting with 0 after the given period.

## Transforming

Functions for transforming (`IAsyncObservable<'a> -> IAsyncObservable<'b>`) an async observable.

## map

Returns an observable sequence whose elements are the result of
invoking the mapper function on each element of the source.

```fs
val map : mapper: ('a -> 'b) -> source: IAsyncObservable<'a> -> IAsyncObservable<'b>
```

**Example:**

```fs
let mapper x = x * 10

let xs = single 42 |> map mapper
```

- **mapi** : ('a*int -> 'b) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **mapAsync** : ('a -> Async<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **mapiAsync** : ('a*int -> Async<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMap** : ('a -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapi** : ('a*int -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapAsync** : ('a -> Async\<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapiAsync** : ('a*int -> Async<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapLatest** : ('a -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapLatestAsync** : ('a -> Async<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **catch** : (exn -> IAsyncObservable<'a>) -> IAsyncObservable<'a> -> IAsyncObservable<'a>

## Filtering

Functions for filtering (`IAsyncObservable<'a> -> IAsyncObservable<'a>`) an async observable.

### filter

Filters the elements of an observable sequence based on a
predicate. Returns an observable sequence that contains elements
from the input sequence that satisfy the condition.

```fs
val filter : predicate: ('a -> bool) -> IAsyncObservable<'a> -> IAsyncObservable<'a>
```

**Example:**

```fs
let predicate x = x < 3

let xs = ofSeq <| seq { 1..5 } |> filter predicate
```

- **filterAsync** : ('a -> Async\<bool\>) -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **distinctUntilChanged** : IAsyncObservable<'a> -> IAsyncObservable<'a>
- **takeUntil** : IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **choose** : ('a -> 'b option) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **chooseAsync** : ('a -> Async<'b option>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

## Aggregating

### scan

Applies an accumulator function over an observable sequence for every value `'a` and returns each intermediate result `'s`. The `initial` seed value is used as the initial accumulator value. Returns an observable sequence containing the accumulated values `'s`.

```fs
val scan : initial: 's -> accumulator: ('s -> 'a -> 's) -> source: IAsyncObservable<'a> -> IAsyncObservable<'s>
```

**Example:**

```fs
let scanner a x = a + x

let xs = ofSeq <| seq { 1..5 } |> scan 0 scanner
```

### scanAsync

Applies an async accumulator function over an observable
sequence and returns each intermediate result. The seed value is
used as the initial accumulator value. Returns an observable
sequence containing the accumulated values.

```fs
val scan : initial: 's -> accumulator: ('s -> 'a -> Async<'s>) -> source: IAsyncObservable<'a> -> IAsyncObservable<'s>
```

**Example:**

```fs
let scannerAsync a x = async { return a + x }

let xs = ofSeq <| seq { 1..5 } |> scanAsync 0 scannerAsync
```

### groupBy

Groups the elements of an observable sequence according to a
specified key mapper function. Returns a sequence of observable
groups, each of which corresponds to a given key.

```fs
val groupBy : keyMapper: ('a -> 'g) -> source: IAsyncObservable<'a> -> IAsyncObservable<IAsyncObservable<'a>>
```

**Example:**

```fs
let xs = ofSeq [1; 2; 3; 4; 5; 6]
        |> groupBy (fun x -> x % 2)
        |> flatMap (fun x -> x)
```

## Combining

Functions for combining multiple async observables into one.

- **merge** : IAsyncObservable<'a> -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **mergeInner** : IAsyncObservable\<IAsyncObservable<'a>\> -> IAsyncObservable<'a>
- **switchLatest** : IAsyncObservable<IAsyncObservable<'a>> -> IAsyncObservable<'a>
- **concat** : seq<IAsyncObservable<'a>> -> IAsyncObservable<'a>
- **startWith** : seq<'a> -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **combineLatest** : IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>
- **withLatestFrom** : IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>
- **zipSeq** : seq<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>

## Time-shifting

Functions for time-shifting (`IAsyncObservable<'a> -> IAsyncObservable<'a>`) an async observable.

- **delay** : int -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **debounce** : int -> IAsyncObservable<'a> -> IAsyncObservable<'a>

### sample

Samples the observable sequence at each interval.

```fs
val sample : msecs: int source: IAsyncObservable<'a> -> IAsyncObservable<'a>
```

## Leaving

Functions for leaving (`IAsyncObservable<'a> -> 'a`) the async observable.

- **toAsyncSeq** : IAsyncObservable<'a> -> AsyncSeq<'a> *(Not available in Fable)*
