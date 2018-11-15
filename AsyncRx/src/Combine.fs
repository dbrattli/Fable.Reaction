namespace Reaction

open Reaction.Core

[<RequireQualifiedAccess>]
module Combine =
    /// Returns an observable sequence that contains the elements of
    /// each given sequences, in sequential order.
    let concatSeq (sources: seq<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        let subscribeAsync (aobv: IAsyncObserver<'a>) =
            let safeObserver = safeObserver aobv

            let innerAgent =
                MailboxProcessor.Start(fun inbox ->
                    let rec messageLoop (innerSubscription : IAsyncDisposable) = async {
                        let! cmd, replyChannel = inbox.Receive ()

                        let obv (replyChannel : AsyncReplyChannel<bool>) n =
                            async {
                                match n with
                                | OnNext x -> do! safeObserver.OnNextAsync x
                                | OnError err ->
                                    do! safeObserver.OnErrorAsync err
                                    replyChannel.Reply false
                                | OnCompleted -> replyChannel.Reply true
                            }

                        let getInnerSubscription = async {
                            match cmd with
                            | InnerObservable xs ->
                                return! AsyncObserver (obv replyChannel) |> xs.SubscribeAsync
                            | Dispose ->
                                do! innerSubscription.DisposeAsync ()
                                replyChannel.Reply true
                                return AsyncDisposable.Empty
                            | _ -> return AsyncDisposable.Empty
                        }

                        do! innerSubscription.DisposeAsync ()
                        let! newInnerSubscription = getInnerSubscription
                        return! messageLoop newInnerSubscription
                    }

                    messageLoop AsyncDisposable.Empty
                )

            async {
                let worker () = async {
                    for source in sources do
                        do! innerAgent.PostAndAsyncReply(fun replyChannel -> InnerObservable source, replyChannel) |> Async.Ignore

                    do! safeObserver.OnCompletedAsync ()
                }
                Async.Start (worker ())

                let cancel () =
                    async {
                        do! innerAgent.PostAndAsyncReply(fun replyChannel -> Dispose, replyChannel) |> Async.Ignore
                    }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Merges an observable sequence of observable sequences into an
    /// observable sequence.
    let mergeInner (source: IAsyncObservable<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        let subscribeAsync (aobv: IAsyncObserver<'a>) =
            let safeObserver = safeObserver aobv
            let refCount = refCountAgent 1 (async {
                do! safeObserver.OnCompletedAsync ()
            })

            let innerAgent =
                let obv = {
                    new IAsyncObserver<'a> with
                        member this.OnNextAsync x = async {
                            do! safeObserver.OnNextAsync x
                        }
                        member this.OnErrorAsync err = async {
                            do! safeObserver.OnErrorAsync err
                        }
                        member this.OnCompletedAsync () = async {
                            refCount.Post Decrease
                        }
                    }

                MailboxProcessor.Start(fun inbox ->
                    let rec messageLoop (innerSubscriptions : IAsyncDisposable list) = async {
                        let! cmd = inbox.Receive()
                        let getInnerSubscriptions = async {
                            match cmd with
                            | InnerObservable xs ->
                                let! inner = xs.SubscribeAsync obv
                                return inner :: innerSubscriptions
                            | Dispose ->
                                for dispose in innerSubscriptions do
                                    do! dispose.DisposeAsync ()
                                return []
                            | _ -> return innerSubscriptions
                        }
                        let! newInnerSubscriptions = getInnerSubscriptions
                        return! messageLoop newInnerSubscriptions
                    }

                    messageLoop []
                )
            async {
                let obv = {
                    new IAsyncObserver<IAsyncObservable<'a>> with
                        member this.OnNextAsync xs = async {
                            refCount.Post Increase
                            InnerObservable xs |> innerAgent.Post
                        }
                        member this.OnErrorAsync err = async {
                            do! safeObserver.OnErrorAsync err
                        }
                        member this.OnCompletedAsync () = async {
                            refCount.Post Decrease
                        }
                    }
                let! dispose = source.SubscribeAsync obv
                let cancel () =
                    async {
                        do! dispose.DisposeAsync ()
                        innerAgent.Post Dispose
                    }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    type Notifications<'a, 'b> =
    | Source of Notification<'a>
    | Other of Notification<'b>

    /// Merges the specified observable sequences into one observable
    /// sequence by combining elements of the sources into tuples.
    /// Returns an observable sequence containing the combined results.
    let combineLatest (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a*'b> =
        let subscribeAsync (aobv: IAsyncObserver<'a*'b>) =
            let safeObserver = safeObserver aobv

            let agent = MailboxProcessor.Start(fun inbox ->
                let rec messageLoop (source: option<'a>) (other: option<'b>) = async {
                    let! cn = inbox.Receive()

                    let onNextOption n =
                        async {
                            match n with
                            | OnNext x ->
                                return Some x
                            | OnError ex ->
                                do! safeObserver.OnErrorAsync ex
                                return None
                            | OnCompleted ->
                                do! safeObserver.OnCompletedAsync ()
                                return None
                        }

                    let! source', other' = async {
                        match cn with
                        | Source n ->
                            let! onNextOptionN = onNextOption n
                            return onNextOptionN, other
                        | Other n ->
                            let! onNextOptionN = onNextOption n
                            return source, onNextOptionN
                    }
                    let c = source' |> Option.bind (fun a -> other' |> Option.map  (fun b -> a, b))
                    match c with
                    | Some x -> do! safeObserver.OnNextAsync x
                    | _ -> ()

                    return! messageLoop source' other'
                }

                messageLoop None None
            )

            async {
                let obvA = AsyncObserver (fun (n : Notification<'a>) -> async { Source n |> agent.Post })
                let! dispose1 = source.SubscribeAsync obvA
                let obvB = AsyncObserver  (fun (n : Notification<'b>) -> async { Other n |> agent.Post })
                let! dispose2 = other.SubscribeAsync obvB

                return AsyncDisposable.Composite [ dispose1; dispose2 ]
            }
        { new IAsyncObservable<'a*'b> with member __.SubscribeAsync o = subscribeAsync o }

    /// Merges the specified observable sequences into one observable
    /// sequence by combining the values into tuples only when the first
    /// observable sequence produces an element. Returns the combined
    /// observable sequence.
    let withLatestFrom (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a*'b> =
        let subscribeAsync (aobv: IAsyncObserver<'a*'b>) =
            let safeObserver = safeObserver aobv

            let agent = MailboxProcessor.Start(fun inbox ->
                let rec messageLoop (latest : option<'b>) = async {
                    let! cn = inbox.Receive()

                    let onNextOption n =
                        async {
                            match n with
                            | OnNext x ->
                                return Some x
                            | OnError ex ->
                                do! safeObserver.OnErrorAsync ex
                                return None
                            | OnCompleted ->
                                do! safeObserver.OnCompletedAsync ()
                                return None
                        }

                    let! source', latest' = async {
                        match cn with
                        | Source n ->
                            let! onNextOptionN = onNextOption n
                            return onNextOptionN, latest
                        | Other n ->
                            let! onNextOptionN = onNextOption n
                            return None, onNextOptionN
                    }
                    let c = source' |> Option.bind (fun a -> latest' |> Option.map  (fun b -> a, b))
                    match c with
                    | Some x -> do! safeObserver.OnNextAsync x
                    | _ -> ()

                    return! messageLoop latest'
                }

                messageLoop None
            )

            async {
                let obvA = AsyncObserver (fun (n : Notification<'a>) -> async { Source n |> agent.Post })
                let obvB = AsyncObserver  (fun (n : Notification<'b>) -> async { Other n |> agent.Post })

                let! dispose1 = other.SubscribeAsync obvB
                let! dispose2 = source.SubscribeAsync obvA

                return AsyncDisposable.Composite [ dispose1; dispose2 ]
            }
        { new IAsyncObservable<'a*'b> with member __.SubscribeAsync o = subscribeAsync o }

    let zipSeq (sequence: seq<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a*'b> =
        let subscribeAsync (aobv: IAsyncObserver<'a*'b>) =
            async {
                let enumerator = sequence.GetEnumerator ()
                let _obv n =
                    async {
                        match n with
                        | OnNext x ->
                            try
                                if enumerator.MoveNext () then
                                    let b =  x, enumerator.Current
                                    do! aobv.OnNextAsync b
                                else
                                    do! aobv.OnCompletedAsync ()
                            with
                            | ex -> do! aobv.OnErrorAsync ex
                        | OnError ex -> do! aobv.OnErrorAsync ex
                        | OnCompleted -> do! aobv.OnCompletedAsync ()

                    }
                return! AsyncObserver _obv |> safeObserver |> source.SubscribeAsync
            }
        { new IAsyncObservable<'a*'b> with member __.SubscribeAsync o = subscribeAsync o }
