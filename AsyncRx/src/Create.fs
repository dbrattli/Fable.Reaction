namespace FSharp.Control

open System.Threading
open System

#if !FABLE_COMPILER
open FSharp.Control
#endif

open Core

open System.Runtime.CompilerServices

[<assembly:InternalsVisibleTo("Tests")>]
do ()


[<RequireQualifiedAccess>]
module internal Create =

    /// Creates an async observable (`AsyncObservable{'a}`) from the
    /// given subscribe function.
    let create (subscribe : IAsyncObserver<'a> -> Async<IAsyncDisposable>) : IAsyncObservable<'a> =
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribe o }

    // Create async observable from async worker function
    let ofAsyncWorker (worker: IAsyncObserver<'a> -> CancellationToken -> Async<unit>) : IAsyncObservable<'a> =
        let subscribeAsync (aobv : IAsyncObserver<_>) : Async<IAsyncDisposable> =
            let disposable, token = canceller ()
            let safeObv = safeObserver aobv

            async {
                Async.Start' (worker safeObv token, token)
                return disposable
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns the async observable sequence whose single element is
    /// the result of the given async workflow.
    let ofAsync (workflow : Async<'a>)  : IAsyncObservable<'a> =
        let subscribeAsync (aobv : IAsyncObserver<_>) : Async<IAsyncDisposable> =
            let safeObv = safeObserver aobv

            async {
                let! result = workflow
                do! safeObv.OnNextAsync result
                do! safeObv.OnCompletedAsync ()
                return AsyncDisposable.Empty
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns an observable sequence containing the single specified element.
    let single (value: 'a) =
        let subscribeAsync (aobv : IAsyncObserver<_>) : Async<IAsyncDisposable> =
            let safeObv = safeObserver aobv

            async {
                do! safeObv.OnNextAsync value
                do! safeObv.OnCompletedAsync ()
                return AsyncDisposable.Empty
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns an observable sequence with no elements.
    let inline empty<'a> () : IAsyncObservable<'a> =
        let subscribeAsync (aobv : IAsyncObserver<_>) : Async<IAsyncDisposable> =
            async {
                do! aobv.OnCompletedAsync ()
                return AsyncDisposable.Empty
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns an empty observable sequence that never completes.
    let inline never<'a> () : IAsyncObservable<'a> =
        let subscribeAsync (_ : IAsyncObserver<_>) : Async<IAsyncDisposable> =
            async {
                return AsyncDisposable.Empty
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns the observable sequence that terminates exceptionally
    /// with the specified exception.
    let inline fail<'a> (error: exn) : IAsyncObservable<'a> =
        ofAsyncWorker (fun obv _ -> async {
            do! obv.OnErrorAsync error
        })

    /// Returns the async observable sequence whose elements are pulled
    /// from the given enumerable sequence.
    let ofSeq (xs: seq<'a>) : IAsyncObservable<'a> =
        ofAsyncWorker (fun obv token -> async {
            for x in xs do
                try
                    do! obv.OnNextAsync x
                with ex ->
                    do! obv.OnErrorAsync ex

            do! obv.OnCompletedAsync ()
        })

#if !FABLE_COMPILER
    /// Convert async sequence into an async observable.
    let ofAsyncSeq (xs: AsyncSeq<'a>) : IAsyncObservable<'a> =
        let subscribeAsync  (aobv : IAsyncObserver<'a>) : Async<IAsyncDisposable> =
            let cancel, token = canceller ()

            async {
                let ie = xs.GetEnumerator ()

                let rec loop () =
                    async {
                        let! result =
                            async {
                                try
                                    let! value = ie.MoveNext ()
                                    return Ok value
                                with
                                | ex -> return Error ex
                            }

                        match result with
                        | Ok notification ->
                            match notification with
                            | Some x ->
                                do! aobv.OnNextAsync x
                                do! loop ()
                            | None ->
                                do! aobv.OnCompletedAsync ()
                        | Error err ->
                            do! aobv.OnErrorAsync err
                    }

                Async.StartImmediate (loop (), token)
                return cancel
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }
#endif

    // Returns an observable sequence that invokes the specified factory
    // function whenever a new observer subscribes.
    let defer (factory: unit -> IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let subscribeAsync  (aobv : IAsyncObserver<'a>) : Async<IAsyncDisposable> =
            async {
                let result =
                    try
                        factory ()
                    with
                    | ex ->
                        fail ex

                return! result.SubscribeAsync aobv
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns an observable sequence that triggers the increasing
    /// sequence starting with 0 after the given msecs, and the after each period.
    let interval (msecs: int) (period: int) : IAsyncObservable<int> =
        let subscribeAsync  (aobv : IAsyncObserver<int>) : Async<IAsyncDisposable> =
            let cancel, token = canceller ()
            async {
                let rec handler msecs next = async {
                    do! Async.Sleep msecs
                    do! aobv.OnNextAsync next

                    if period > 0 then
                        return! handler period (next + 1)
                    else
                        do! aobv.OnCompletedAsync ()
                }

                Async.Start' (handler msecs 0, token)
                return cancel
            }

        { new IAsyncObservable<int> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns an observable sequence that triggers the value 0
    /// after the given duetime in milliseconds.
    let timer (dueTime: int) : IAsyncObservable<int> =
        interval dueTime 0
