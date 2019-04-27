namespace FSharp.Control

type IAsyncDisposable =
    abstract member DisposeAsync: unit -> Async<unit>

type IAsyncObserver<'a> =
    abstract member OnNextAsync: 'a -> Async<unit>
    abstract member OnErrorAsync: exn -> Async<unit>
    abstract member OnCompletedAsync: unit -> Async<unit>

type IAsyncObservable<'a> =
    abstract member SubscribeAsync: IAsyncObserver<'a> -> Async<IAsyncDisposable>

type Notification<'a> =
    | OnNext of 'a
    | OnError of exn
    | OnCompleted
