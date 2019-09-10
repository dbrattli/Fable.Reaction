namespace FSharp.Control

open FSharp.Control.Core
open System.Collections.Generic

[<RequireQualifiedAccess>]
module internal Filter =
    /// Applies the given async function to each element of the stream and
    /// returns the stream comprised of the results for each element
    /// where the function returns Some with some value.
    let chooseAsync (chooser: 'a -> Async<'b option>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        let subscribeAsync (obvAsync : IAsyncObserver<'b>) =
            async {
                let _obv =
                    { new IAsyncObserver<'a> with
                        member this.OnNextAsync x = async {
                            // Let exceptions bubble to the top
                            let! result = chooser x
                            match result with
                            | Some b ->
                                do! obvAsync.OnNextAsync b
                            | None -> ()
                        }
                        member __.OnErrorAsync err = obvAsync.OnErrorAsync err

                        member __.OnCompletedAsync () = obvAsync.OnCompletedAsync ()

                    }
                return! source.SubscribeAsync _obv
            }

        { new IAsyncObservable<'b> with member __.SubscribeAsync o = subscribeAsync o }

    /// Applies the given function to each element of the stream and
    /// returns the stream comprised of the results for each element
    /// where the function returns Some with some value.
    let choose (chooser: 'a -> 'b option) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        chooseAsync  (fun x -> async { return chooser x }) source

    /// Filters the elements of an observable sequence based on an async
    /// predicate. Returns an observable sequence that contains elements
    /// from the input sequence that satisfy the condition.
    let filterAsync (predicate: 'a -> Async<bool>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let predicate' a = async {
            let! result = predicate a
            match result with
            | true -> return Some a
            | _ -> return None
        }
        chooseAsync predicate' source

    /// Filters the elements of an observable sequence based on a
    /// predicate. Returns an observable sequence that contains elements
    /// from the input sequence that satisfy the condition.
    let filter (predicate: 'a -> bool) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        filterAsync (fun x -> async { return predicate x }) source

    /// Return an observable sequence only containing the distinct
    /// contiguous elementsfrom the source sequence.
    let distinctUntilChanged (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let subscribeAsync (aobv : IAsyncObserver<'a>) =
            let safeObserver = safeObserver aobv
            let agent = MailboxProcessor.Start(fun inbox ->
                let rec messageLoop (latest : Notification<'a>) = async {
                    let! n = inbox.Receive()

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
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Bypasses a specified number of elements in an observable sequence
    /// and then returns the remaining elements.
    let skip (count: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let subscribeAsync (obvAsync : IAsyncObserver<'a>) =
            let safeObv = safeObserver obvAsync

            async {
                let mutable remaining = count

                let _obv (n : Notification<'a>) =
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
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns a specified number of contiguous elements from the start of
    /// an observable sequence.
    let take (count: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let subscribeAsync (obvAsync : IAsyncObserver<'a>) =
            let safeObv = safeObserver obvAsync

            async {
                let mutable remaining = count

                let _obv (n : Notification<'a>) =
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
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns a specified number of contiguous elements from the end of an
    /// observable sequence.
    let takeLast (count: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let subscribeAsync (obvAsync : IAsyncObserver<'a>) =
            let safeObv = safeObserver obvAsync
            let mutable queue = List<'a> ()

            async {
                let _obv (n : Notification<'a>) =
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
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns the values from the source observable sequence until the
    /// other observable sequence produces a value.
    let takeUntil (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let subscribeAsync (obvAsync : IAsyncObserver<'a>) =
            let safeObv = safeObserver obvAsync

            async {
                let _obv (n : Notification<'b>) =
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
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }