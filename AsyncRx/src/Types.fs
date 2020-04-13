namespace FSharp.Control

type IAsyncDisposable =
    abstract member DisposeAsync: unit -> Async<unit>

type IAsyncObserver<'T> =
    abstract member OnNextAsync: 'T -> Async<unit>
    abstract member OnErrorAsync: exn -> Async<unit>
    abstract member OnCompletedAsync: unit -> Async<unit>

type IAsyncObservable<'T> =
    abstract member SubscribeAsync: IAsyncObserver<'T> -> Async<IAsyncDisposable>

type Notification<'T> =
    | OnNext of 'T
    | OnError of exn
    | OnCompleted

type Stream<'TSource, 'TResult> = IAsyncObservable<'TSource> -> IAsyncObservable<'TResult>
type Stream<'TSource> = Stream<'TSource, 'TSource>

[<AutoOpen>]
module Streams =
    let (>=>) (source: Stream<'T1, 'T2>) (other: Stream<'T2, 'T3>) = source >> other


