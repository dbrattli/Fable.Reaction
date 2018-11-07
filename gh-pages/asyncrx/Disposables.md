# Disposables

Asynchronous disposables are similar to [IDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=netcore-2.1) but with the difference that it encapsulates an async dispose method called `DisposeAsync`.

```fs
type AsyncDisposable (cancel) =
    interface IAsyncDisposable with
        member this.DisposeAsync () =
            async {
                do! cancel ()
            }
```

In additon the following static members have been defined:

- **Create** : `(unit -> Async<unit>) -> IAsyncDisposable`, creates an `IAsyncDisposable` from an async cancel function. Same instantiating the object, but the return value here is anonymous.
- **Empty** : `unit -> IAsyncDisposable`, creates an empty disposable that will do nothing if it's disposed.
- **Composite** : `seq<IAsyncDisposable> -> IAsyncDisposable`, creates a disposable from a sequence of disposables. If disposed then all the contained disposables will also be disposed.

The `SubscribeAsync` method for async observables returns an async disposable, and to cancel the subscription you need to call the `DisposeAsync` method.

```fs
let xs = ofSeq <| seq { 1 .. 5 }
let obv = MyObserver<int>()
let! subscription = xs.SubscribeAsync obv

// Unsubscribe
do! subscription.DisposeAsync ()
```
