namespace FSharp.Control

open System.Threading
open FSharp.Control.Core

[<RequireQualifiedAccess>]
 module internal Aggregation =
    /// Applies an async accumulator function over an observable sequence and returns each intermediate result. The seed
    /// value is used as the initial accumulator value. Returns an observable sequence containing the accumulated
    /// values.
    let scanInitAsync (initial: 'TState) (accumulator: 'TState -> 'TSource -> Async<'TState>) (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TState> =
        let subscribeAsync (aobv : IAsyncObserver<'TState>) =
            let safeObserver = safeObserver aobv
            let mutable state = initial

            async {
                let obv n =
                    async {
                        match n with
                        | OnNext x ->
                            try
                                let! state' = accumulator state x
                                state <- state'
                                do! safeObserver.OnNextAsync state
                            with
                            | err -> do! safeObserver.OnErrorAsync err
                        | OnError e -> do! safeObserver.OnErrorAsync e
                        | OnCompleted -> do! safeObserver.OnCompletedAsync ()
                    }
                return! AsyncObserver obv |> source.SubscribeAsync
            }
        { new IAsyncObservable<'TState> with member __.SubscribeAsync o = subscribeAsync o }

    /// Applies an async accumulator function over an observable sequence and returns each intermediate result. The
    /// first value is used as the initial accumulator value. Returns an observable sequence containing the accumulated
    /// values.
    let scanAsync (accumulator: 'TSource -> 'TSource -> Async<'TSource>) (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let subscribeAsync (aobv : IAsyncObserver<'TSource>) =
            let safeObserver = safeObserver aobv
            let mutable states = None

            async {
                let obv n =
                    async {
                        match n with
                        | OnNext x ->
                            match states with
                            | Some state ->
                                try
                                    let! state' = accumulator state x
                                    states <- Some state'
                                    do! safeObserver.OnNextAsync state
                                with
                                | err -> do! safeObserver.OnErrorAsync err
                            | None ->
                                states <- Some x
                        | OnError e -> do! safeObserver.OnErrorAsync e
                        | OnCompleted -> do! safeObserver.OnCompletedAsync ()
                    }
                return! AsyncObserver obv |> source.SubscribeAsync
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }


    /// Groups the elements of an observable sequence according to a specified key mapper function. Returns a sequence
    /// of observable groups, each of which corresponds to a given key.
    let groupBy (keyMapper: 'TSource -> 'TKey) (source: IAsyncObservable<'TSource>) : IAsyncObservable<IAsyncObservable<'TSource>> =
        let subscribeAsync (aobv: IAsyncObserver<IAsyncObservable<'TSource>>) =
            let cts = new CancellationTokenSource()
            let agent = MailboxProcessor.Start((fun inbox ->
                let rec messageLoop ((groups, disposed) : Map<'TKey, IAsyncObserver<'TSource>>*bool) = async {
                    let! n = inbox.Receive ()

                    if disposed then
                        return! messageLoop (Map.empty, true)

                    let! newGroups, disposed =
                        async {
                            match n with
                            | OnNext x ->
                                let groupKey = keyMapper x
                                let! newGroups = async {
                                    match groups.TryFind groupKey with
                                    | Some group ->
                                        do! group.OnNextAsync x
                                        return groups, false
                                    | None ->
                                        let obv, obs = Subjects.singleSubject ()
                                        do! aobv.OnNextAsync obs
                                        do! obv.OnNextAsync x
                                        return groups.Add (groupKey, obv), false
                                }
                                return newGroups
                            | OnError ex ->
                                for entry in groups do
                                    do! entry.Value.OnErrorAsync ex
                                do! aobv.OnErrorAsync ex
                                return Map.empty, true
                            | OnCompleted ->
                                for entry in groups do
                                    do! entry.Value.OnCompletedAsync ()
                                do! aobv.OnCompletedAsync ()
                                return Map.empty, true
                        }

                    return! messageLoop (newGroups, disposed)
                }

                messageLoop (Map.empty, false)), cts.Token)

            async {
                let obv (n : Notification<'TSource>) =
                    async {
                        agent.Post n
                    }
                let! subscription = AsyncObserver obv |> source.SubscribeAsync
                let cancel () = async {
                    cts.Cancel()
                    do! subscription.DisposeAsync ()
                }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<IAsyncObservable<'TSource>> with member __.SubscribeAsync o = subscribeAsync o }