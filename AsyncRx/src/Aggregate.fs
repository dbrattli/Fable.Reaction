namespace Reaction.AsyncRx

open System.Threading
open Reaction.AsyncRx.Core

[<RequireQualifiedAccess>]
module Aggregation =
    /// Applies an async accumulator function over an observable
    /// sequence and returns each intermediate result. The seed value is
    /// used as the initial accumulator value. Returns an observable
    /// sequence containing the accumulated values.
    let scanAsync (initial: 's) (accumulator: 's -> 'a -> Async<'s>) (source: IAsyncObservable<'a>) : IAsyncObservable<'s> =
        let subscribeAsync (aobv : IAsyncObserver<'s>) =
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
        { new IAsyncObservable<'s> with member __.SubscribeAsync o = subscribeAsync o }

    /// Applies an accumulator function over an observable sequence and
    /// returns each intermediate result. The seed value is used as the
    /// initial accumulator value. Returns an observable sequence
    /// containing the accumulated values.
    let scan (initial : 's) (scanner:'s -> 'a -> 's) (source: IAsyncObservable<'a>) : IAsyncObservable<'s> =
        scanAsync initial (fun s x -> async { return scanner s x } ) source

    /// Groups the elements of an observable sequence according to a
    /// specified key mapper function. Returns a sequence of observable
    /// groups, each of which corresponds to a given key.
    let groupBy (keyMapper: 'a -> 'g) (source: IAsyncObservable<'a>) : IAsyncObservable<IAsyncObservable<'a>> =
        let subscribeAsync (aobv: IAsyncObserver<IAsyncObservable<'a>>) =
            let cts = new CancellationTokenSource()
            let agent = MailboxProcessor.Start((fun inbox ->
                let rec messageLoop ((groups, disposed) : Map<'g, IAsyncObserver<'a>>*bool) = async {
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
                                        //printfn "Found group: %A" groupKey
                                        do! group.OnNextAsync x
                                        return groups, false
                                    | None ->
                                        //printfn "New group: %A" groupKey
                                        let obv, obs = Streams.singleStream ()
                                        do! aobv.OnNextAsync obs
                                        do! obv.OnNextAsync x
                                        return groups.Add (groupKey, obv), false
                                }
                                return newGroups
                            | OnError ex ->
                                //printfn "%A" n

                                for entry in groups do
                                    do! entry.Value.OnErrorAsync ex
                                do! aobv.OnErrorAsync ex
                                return Map.empty, true
                            | OnCompleted ->
                                //printfn "%A" n

                                for entry in groups do
                                    do! entry.Value.OnCompletedAsync ()
                                do! aobv.OnCompletedAsync ()
                                return Map.empty, true
                        }

                    return! messageLoop (newGroups, disposed)
                }

                messageLoop (Map.empty, false)), cts.Token)

            async {
                let obv (n : Notification<'a>) =
                    async {
                        agent.Post n
                    }
                let! subscription = AsyncObserver obv |> source.SubscribeAsync
                let cancel () = async {
                    do! subscription.DisposeAsync ()
                    cts.Cancel()
                }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<IAsyncObservable<'a>> with member __.SubscribeAsync o = subscribeAsync o }