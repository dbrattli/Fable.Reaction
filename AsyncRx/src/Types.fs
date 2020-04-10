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
