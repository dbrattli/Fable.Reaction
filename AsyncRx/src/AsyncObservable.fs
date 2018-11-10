namespace Reaction

#if !FABLE_COMPILER
open FSharp.Control
#endif

/// Overloads and extensions for AsyncObservable
[<AutoOpen>]
module AsyncObservable =
    type IAsyncObservable<'a> with
        /// Repeat each element of the sequence n times
        /// Subscribes the async observer to the async observable,
        /// ignores the disposable
        member this.RunAsync (obv: IAsyncObserver<'a>) = async {
            let! _ = this.SubscribeAsync obv
            return ()
        }

        /// Subscribes the observer function (`Notification{'a} -> Async{unit}`)
        /// to the AsyncObservable, ignores the disposable.
        member this.RunAsync<'a> (obv: Notification<'a> -> Async<unit>) = async {
            do! this.SubscribeAsync (AsyncObserver obv) |> Async.Ignore
        }

        /// Subscribes the async observer function (`Notification{'a} -> Async{unit}`)
        /// to the AsyncObservable
        member this.SubscribeAsync<'a> (obv: Notification<'a> -> Async<unit>) = async {
            let! disposable = this.SubscribeAsync (AsyncObserver obv)
            return disposable
        }

    /// Returns an observable sequence that contains the elements of
    /// the given sequences.
    let inline (++) source other = Combine.concat [source; other]

/// A single module that contains all the operators. Nicer and shorter way than writing
/// AsyncObservable. We want to prefix our operators so we don't mix e.g. `map` with other modules.
module AsyncRx =

  // AggregateRegion

    /// Groups the elements of an observable sequence according to a
    /// specified key mapper function. Returns a sequence of observable
    /// groups, each of which corresponds to a given key.
    let inline groupBy (keyMapper: 'a -> 'g) (source: IAsyncObservable<'a>) : IAsyncObservable<IAsyncObservable<'a>> =
        Aggregation.groupBy keyMapper source

    /// Applies an accumulator function over an observable sequence and
    /// returns each intermediate result. The seed value is used as the
    /// initial accumulator value. Returns an observable sequence
    /// containing the accumulated values.
    let inline scan (initial: 's) (accumulator: 's -> 'a -> 's) (source: IAsyncObservable<'a>) : IAsyncObservable<'s> =
        Aggregation.scan initial accumulator source

    /// Applies an async accumulator function over an observable
    /// sequence and returns each intermediate result. The seed value is
    /// used as the initial accumulator value. Returns an observable
    /// sequence containing the accumulated values.
    let inline scanAsync (initial: 's) (accumulator: 's -> 'a -> Async<'s>) (source: IAsyncObservable<'a>) : IAsyncObservable<'s> =
        Aggregation.scanAsync initial accumulator source

  // CombineRegion

    /// Merges the specified observable sequences into one observable
    /// sequence by combining elements of the sources into tuples.
    /// Returns an observable sequence containing the combined results.
    let inline combineLatest (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a*'b> =
        Combine.combineLatest other source

    /// Returns an observable sequence that contains the elements of
    /// each given sequences, in sequential order.
    let inline concat (sources: seq<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        Combine.concat sources

    /// Merges an observable sequence of observable sequences into an
    /// observable sequence.
    let inline mergeInner (source: IAsyncObservable<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        Combine.mergeInner source

    /// Merges an observable sequence with another observable sequences.
    let inline merge (other : IAsyncObservable<'a>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Create.ofSeq [source; other] |> mergeInner

    /// Prepends a sequence of values to an observable sequence.
    /// Returns the source sequence prepended with the specified values.
    let inline startWith (items : seq<'a>) (source: IAsyncObservable<'a>) =
        concat [Create.ofSeq items; source]

    /// Merges the specified observable sequences into one observable
    /// sequence by combining the values into tuples only when the first
    /// observable sequence produces an element. Returns the combined
    /// observable sequence.
    let inline withLatestFrom (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a*'b> =
        Combine.withLatestFrom other source

    /// Zip given sequence with source. Combines one and one item from each stream into one tuple.
    let inline zipSeq (sequence: seq<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a*'b> =
        Combine.zipSeq sequence source

  // CreateRegion

    /// Creates an async observable (`AsyncObservable{'a}`) from the
    /// given subscribe function.
    let inline create (subscribe : IAsyncObserver<'a> -> Async<IAsyncDisposable>) : IAsyncObservable<'a> =
        Create.create subscribe

    // Returns an observable sequence that invokes the specified factory
    // function whenever a new observer subscribes.
    let inline defer (factory: unit -> IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Create.defer factory

    /// Returns an observable sequence with no elements.
    let inline empty<'a> () : IAsyncObservable<'a> =
        Create.empty<'a> ()

    /// Returns an empty observable sequence that never completes.
    let inline never<'a> () : IAsyncObservable<'a> =
        Create.never<'a> ()

    /// Returns the observable sequence that terminates exceptionally
    /// with the specified exception.
    let inline fail<'a> (error: exn) : IAsyncObservable<'a> =
        Create.fail<'a> error

    /// Returns an observable sequence that triggers the increasing
    /// sequence starting with 0 after the given period.
    let inline interval (msecs: int) (period: int) : IAsyncObservable<int> =
        Create.interval msecs period

    /// Returns the async observable sequence whose single element is
    /// the result of the given async workflow.
    let inline ofAsync (workflow: Async<'a>)  : IAsyncObservable<'a> =
        Create.ofAsync workflow

    #if !FABLE_COMPILER
    /// Convert async sequence into an async observable.
    let inline ofAsyncSeq (xs: AsyncSeq<'a>) : IAsyncObservable<'a> =
        Create.ofAsyncSeq xs
    #endif

    /// Returns the async observable sequence whose elements are pulled
    /// from the given enumerable sequence.
    let inline ofSeq (xs: seq<'a>) : IAsyncObservable<'a> =
        Create.ofSeq xs

    /// Returns an observable sequence containing the single specified
    /// element.
    let inline single (x : 'a) : IAsyncObservable<'a> =
        upcast ValueObservable x

    /// Returns an observable sequence that triggers the value 0
    /// after the given duetime.
    let inline timer (dueTime: int) : IAsyncObservable<int> =
        Create.timer dueTime

  // FilterRegion

    /// Applies the given function to each element of the stream and
    /// returns the stream comprised of the results for each element
    /// where the function returns Some with some value.
    let inline choose (chooser: 'a -> 'b option) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Filter.choose chooser source

    /// Applies the given async function to each element of the stream and
    /// returns the stream comprised of the results for each element
    /// where the function returns Some with some value.
    let inline chooseAsync (chooser: 'a -> Async<'b option>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Filter.chooseAsync chooser source

    /// Return an observable sequence only containing the distinct
    /// contiguous elementsfrom the source sequence.
    let inline distinctUntilChanged (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Filter.distinctUntilChanged source

    /// Filters the elements of an observable sequence based on a
    /// predicate. Returns an observable sequence that contains elements
    /// from the input sequence that satisfy the condition.
    let inline filter (predicate: 'a -> bool) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Filter.filter predicate source

    /// Filters the elements of an observable sequence based on an async
    /// predicate. Returns an observable sequence that contains elements
    /// from the input sequence that satisfy the condition.
    let inline filterAsync (predicate: 'a -> Async<bool>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Filter.filterAsync predicate source

    /// Returns the values from the source observable sequence until the
    /// other observable sequence produces a value.
    let inline takeUntil (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Filter.takeUntil other source

  // LeaveRegion
    #if !FABLE_COMPILER
    /// Convert async observable to async sequence, non-blocking.
    /// Producer will be awaited until item is consumed by the async
    /// enumerator.
    let inline toAsyncSeq (source: IAsyncObservable<'a>) : AsyncSeq<'a> =
        Leave.toAsyncSeq source
    #endif

  // TimeshiftRegion

    /// Ignores values from an observable sequence which are followed by
    /// another value before the given timeout.
    let inline debounce (msecs: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Timeshift.debounce msecs source

    /// Time shifts the observable sequence by the given timeout. The
    /// relative time intervals between the values are preserved.
    let inline delay (msecs: int) (source: IAsyncObservable<_>) : IAsyncObservable<'a> =
        Timeshift.delay msecs source

    /// Samples the observable sequence at each interval.
    let inline sample (msecs: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Timeshift.sample msecs source

   // TransformRegion

    /// Returns an observable sequence containing the first sequence's
    /// elements, followed by the elements of the handler sequence in
    /// case an exception occurred.
    let inline catch (handler: exn -> IAsyncObservable<'a>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Transformation.catch handler source

    /// Projects each element of an observable sequence into an
    /// observable sequence and merges the resulting observable
    /// sequences back into one observable sequence.
    let inline flatMap (mapper:'a -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.flatMap mapper source

    /// Asynchronously projects each element of an observable sequence
    /// into an observable sequence and merges the resulting observable
    /// sequences back into one observable sequence.
    let inline flatMapAsync (mapperAsync:'a -> Async<IAsyncObservable<'b>>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.flatMapAsync mapperAsync source

    /// Projects each element of an observable sequence into an
    /// observable sequence by incorporating the element's
    /// index on each element of the source. Merges the resulting
    /// observable sequences back into one observable sequence.
    let inline flatMapi (mapper:'a*int -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.flatMapi mapper source

    /// Asynchronously projects each element of an observable sequence
    /// into an observable sequence by incorporating the element's
    /// index on each element of the source. Merges the resulting
    /// observable sequences back into one observable sequence.
    let inline flatMapiAsync  (mapperAsync:'a*int -> Async<IAsyncObservable<'b>>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.flatMapiAsync mapperAsync source

    /// Transforms the items emitted by an source sequence into
    /// observable streams, and mirror those items emitted by the
    /// most-recently transformed observable sequence.
    let inline flatMapLatest (mapper: 'a -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.flatMapLatest mapper source

    /// Asynchronosly transforms the items emitted by an source sequence
    /// into observable streams, and mirror those items emitted by the
    /// most-recently transformed observable sequence.
    let inline flatMapLatestAsync (mapperAsync: 'a -> Async<IAsyncObservable<'b>>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.flatMapLatestAsync mapperAsync source

    /// Returns an observable sequence whose elements are the result of
    /// invoking the mapper function on each element of the source.
    let inline map (mapper:'a -> 'b) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.map mapper source

    /// Returns an observable sequence whose elements are the result of
    /// invoking the async mapper function on each element of the source.
    let inline mapAsync (mapperAsync: 'a -> Async<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.mapAsync mapperAsync source

    /// Returns an observable sequence whose elements are the result of
    /// invoking the mapper function and incorporating the element's
    /// index on each element of the source.
    let inline mapi (mapper:'a*int -> 'b) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.mapi mapper source

    /// Returns an observable sequence whose elements are the result of
    /// invoking the async mapper function by incorporating the element's
    /// index on each element of the source.
    let inline mapiAsync (mapper:'a*int -> Async<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transformation.mapiAsync mapper source

    /// Transforms an observable sequence of observable sequences into
    /// an observable sequence producing values only from the most
    /// recent observable sequence.
    let inline switchLatest (source: IAsyncObservable<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        Transformation.switchLatest source

    /// Share a single subscription among multple observers.
    /// Returns a new Observable that multicasts (shares) the original
    /// Observable. As long as there is at least one Subscriber this
    /// Observable will be subscribed and emitting data. When all
    /// subscribers have unsubscribed it will unsubscribe from the source
    /// Observable.
    let inline share (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Transformation.share source

  // StreamsRegion

    /// A stream is both an observable sequence as well as an observer.
    /// Each notification is broadcasted to all subscribed observers.
    let inline stream<'a> () : IAsyncObserver<'a> * IAsyncObservable<'a> =
        Streams.stream<'a> ()

    /// A mailbox stream is a subscribable mailbox. Each message is
    /// broadcasted to all subscribed observers.
    let inline mbStream<'a> () : MailboxProcessor<Notification<'a>>*IAsyncObservable<'a> =
        Streams.mbStream<'a> ()

    /// A cold stream that only supports a single subscriber. Will await the
    /// caller if no-one is subscribing.
    let inline singleStream<'a> () : IAsyncObserver<'a> * IAsyncObservable<'a> =
        Streams.singleStream<'a> ()
