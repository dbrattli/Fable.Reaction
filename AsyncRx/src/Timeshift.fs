namespace FSharp.Control

open System
open System.Threading;

open FSharp.Control.Core


[<RequireQualifiedAccess>]
module internal Timeshift =

    /// Time shifts the observable sequence by the given timeout. The
    /// relative time intervals between the values are preserved.
    let delay (msecs: int) (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let cts = new CancellationTokenSource()

        let subscribeAsync (aobv : IAsyncObserver<'TSource>) =
            let agent = MailboxProcessor.Start((fun inbox ->
                let rec messageLoop state = async {
                    let! n, dueTime = inbox.Receive()

                    let diff : TimeSpan = dueTime - DateTime.UtcNow
                    let msecs = Convert.ToInt32 diff.TotalMilliseconds
                    if msecs > 0 then
                        do! Async.Sleep msecs

                    match n with
                    | OnNext x -> do! aobv.OnNextAsync x
                    | OnError ex -> do! aobv.OnErrorAsync ex
                    | OnCompleted -> do! aobv.OnCompletedAsync ()

                    return! messageLoop state
                }
                messageLoop (0, 0)), cts.Token)

            async {
                let obv n =
                    async {
                        let dueTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(float msecs)
                        agent.Post (n, dueTime)
                    }
                let! subscription = AsyncObserver obv |> source.SubscribeAsync
                let cancel () = async {
                    cts.Cancel()
                    do! subscription.DisposeAsync ()
                }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }

    /// Ignores values from an observable sequence which are followed by
    /// another value before the given timeout.
    let debounce msecs (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let subscribeAsync (aobv: IAsyncObserver<'TSource>) =
            let safeObv, autoDetach = autoDetachObserver aobv
            let infinite = Seq.initInfinite id

            let agent = MailboxProcessor.Start(fun inbox ->
                let rec messageLoop currentIndex = async {
                    let! n, index = inbox.Receive ()

                    let! newIndex = async {
                        match n, index with
                        | OnNext x, idx when idx = currentIndex ->
                            do! safeObv.OnNextAsync x
                            return index
                        | OnNext _, _ ->
                            if index > currentIndex then
                                return index
                            else
                                return currentIndex
                        | OnError ex, _ ->
                            do! safeObv.OnErrorAsync ex
                            return currentIndex
                        | OnCompleted, _ ->
                            do! safeObv.OnCompletedAsync ()
                            return currentIndex

                    }
                    return! messageLoop newIndex
                }

                messageLoop -1
            )

            async {
                let indexer = infinite.GetEnumerator ()

                let obv (n: Notification<'TSource>) =
                    async {
                        indexer.MoveNext () |> ignore
                        let index = indexer.Current
                        agent.Post (n, index)

                        let worker = async {
                            do! Async.Sleep msecs
                            agent.Post (n, index)
                        }

                        Async.Start' worker
                    }
                let! dispose = AsyncObserver obv |> source.SubscribeAsync |> autoDetach

                let cancel () =
                    async {
                        do! dispose.DisposeAsync ()
                        agent.Post (OnCompleted, 0)
                    }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }

    /// Samples the observable sequence at each interval.
    let sample msecs (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let timer = Create.interval msecs msecs

        if msecs > 0 then
            Combine.withLatestFrom source timer |> Transform.map (fun (_, source) -> source)
        else
            source