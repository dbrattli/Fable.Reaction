namespace FSharp.Control

open FSharp.Control.Core
open System.Collections.Generic

[<RequireQualifiedAccess>]
module internal Filter =
    /// Applies the given async function to each element of the stream and returns the stream comprised of the results
    /// for each element where the function returns Some with some value.
    let chooseAsync (chooser: 'TSource -> Async<'TResult option>) : Stream<'TSource, 'TResult> =
        Transform.transformAsync (fun next a -> async {
            match! chooser a with
            | Some b -> return! next b
            | None -> return ()
        })

    /// Applies the given function to each element of the stream and returns the stream comprised of the results for
    /// each element where the function returns Some with some value.
    let choose (chooser: 'TSource -> 'TResult option) : Stream<'TSource, 'TResult> =
        Transform.transformAsync (fun next a ->
            match chooser a with
            | Some b -> next b
            | None -> Async.empty
        )

    /// Filters the elements of an observable sequence based on an async predicate. Returns an observable sequence that
    /// contains elements from the input sequence that satisfy the condition.
    let filterAsync (predicate: 'TSource -> Async<bool>) : Stream<'TSource> =
        Transform.transformAsync (fun next a -> async {
            match! predicate a with
            | true -> return! next a
            | _ -> return ()
        })


    /// Filters the elements of an observable sequence based on a predicate. Returns an observable sequence that
    /// contains elements from the input sequence that satisfy the condition.
    let filter (predicate: 'TSource -> bool) : Stream<'TSource> =
        Transform.transformAsync (fun next a ->
            match predicate a with
            | true -> next a
            | _ -> Async.empty
        )

    /// Return an observable sequence only containing the distinct contiguous elementsfrom the source sequence.
    let distinctUntilChanged (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let subscribeAsync (aobv : IAsyncObserver<'TSource>) =
            let safeObserver = safeObserver aobv
            let agent = MailboxProcessor.Start(fun inbox ->
                let rec messageLoop (latest: Notification<'TSource>) = async {
                    let! n = inbox.Receive ()

                    let! latest' = async {
                        match n with
                        | OnNext x ->
                            if n <> latest then
                                try
                                    do! safeObserver.OnNextAsync x
                                with
                                | ex -> do! safeObserver.OnErrorAsync ex
                        | OnError err ->
                            do! safeObserver.OnErrorAsync err
                        | OnCompleted ->
                            do! safeObserver.OnCompletedAsync ()
                        return n
                    }

                    return! messageLoop latest'
                }

                messageLoop OnCompleted // Use as sentinel value as it will not match any OnNext value
            )

            async {
                let obv n =
                    async {
                        agent.Post n
                    }
                return! AsyncObserver obv |> source.SubscribeAsync
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }

    /// Bypasses a specified number of elements in an observable sequence and then returns the remaining elements.
    let skip (count: int) (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let subscribeAsync (obvAsync : IAsyncObserver<'TSource>) =
            let safeObv = safeObserver obvAsync

            async {
                let mutable remaining = count

                let _obv (n : Notification<'TSource>) =
                    async {
                        match n with
                        | OnNext x ->
                            if remaining <= 0 then
                                do! safeObv.OnNextAsync x
                            else
                                remaining <- remaining - 1

                        | OnError ex -> do! safeObv.OnErrorAsync ex
                        | OnCompleted -> do! safeObv.OnCompletedAsync ()
                    }

                return! source.SubscribeAsync (AsyncObserver.Create _obv)
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns a specified number of contiguous elements from the start of an observable sequence.
    let take (count: int) (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let subscribeAsync (obvAsync : IAsyncObserver<'TSource>) =
            let safeObv = safeObserver obvAsync

            async {
                let mutable remaining = count

                let _obv (n : Notification<'TSource>) =
                    async {
                        match n with
                        | OnNext x ->
                            if remaining > 0 then
                                do! safeObv.OnNextAsync x
                                remaining <- remaining - 1
                            if remaining = 0 then
                                do! safeObv.OnCompletedAsync ()
                                remaining <- remaining - 1

                        | OnError ex -> do! safeObv.OnErrorAsync ex
                        | OnCompleted -> do! safeObv.OnCompletedAsync ()
                    }

                return! source.SubscribeAsync (AsyncObserver.Create _obv)
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns a specified number of contiguous elements from the end of an observable sequence.
    let takeLast (count: int) (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let subscribeAsync (obvAsync : IAsyncObserver<'TSource>) =
            let safeObv = safeObserver obvAsync
            let mutable queue = List<'TSource> ()

            async {
                let _obv (n : Notification<'TSource>) =
                    async {
                        match n with
                        | OnNext x ->
                            queue.Add x
                            if queue.Count > count then
                                queue.RemoveAt 0
                        | OnError ex -> do! safeObv.OnErrorAsync ex
                        | OnCompleted ->
                            for item in queue do
                                do! safeObv.OnNextAsync item
                            do! safeObv.OnCompletedAsync ()
                    }

                return! source.SubscribeAsync (AsyncObserver.Create _obv)
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns the values from the source observable sequence until the other observable sequence produces a value.
    let takeUntil (other: IAsyncObservable<'TResult>) (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let subscribeAsync (obvAsync : IAsyncObserver<'TSource>) =
            let safeObv = safeObserver obvAsync

            async {
                let _obv (n : Notification<'TResult>) =
                    async {
                        match n with
                        | OnNext x ->
                            do! safeObv.OnCompletedAsync ()
                        | OnError ex -> do! safeObv.OnErrorAsync ex
                        | OnCompleted -> ()
                    }

                let! sub2 = AsyncObserver _obv |> other.SubscribeAsync
                let! sub1 = source.SubscribeAsync safeObv

                return AsyncDisposable.Composite [ sub1; sub2 ]
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }