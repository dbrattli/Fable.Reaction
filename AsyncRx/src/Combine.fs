namespace Reaction

open System.Collections.Generic
open Reaction.Core

[<RequireQualifiedAccess>]
module Combine =
    type Key = int
    type Model<'a> = {
        Subscriptions: Map<Key, IAsyncDisposable>
        Queue: List<IAsyncObservable<'a>>
        IsStopped: bool
        Key: Key
    }

    [<RequireQualifiedAccess>]
    type Msg<'a> =
        | InnerObservable of IAsyncObservable<'a>
        | InnerCompleted of Key
        | OuterCompleted
        | Dispose

    let mergeInner (maxConcurrent: int) (source: IAsyncObservable<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        let subscribeAsync (aobv: IAsyncObserver<'a>) =
            let safeObv = safeObserver aobv

            let initialModel = {
                Subscriptions = Map.empty
                Queue = new List<IAsyncObservable<'a>> ()
                IsStopped = false
                Key = 0
            }

            let agent =
                MailboxProcessor.Start(fun inbox ->
                    let obv key = {
                        new IAsyncObserver<'a> with
                            member this.OnNextAsync x = async {
                                do! safeObv.OnNextAsync x
                            }
                            member this.OnErrorAsync err = async {
                                do! safeObv.OnErrorAsync err
                            }
                            member this.OnCompletedAsync () = async {
                                Msg.InnerCompleted key |> inbox.Post
                            }
                        }

                    let update msg model =
                        async {
                            match msg with
                            | Msg.InnerObservable xs ->
                                //printfn "InnerObservable: maxConcurrent=%A" maxConcurrent
                                if maxConcurrent = 0 || model.Subscriptions.Count < maxConcurrent then
                                    //printfn "InnerObservable: Subscribe: %A" model.Key
                                    let! inner = xs.SubscribeAsync (obv model.Key)
                                    return { model with Subscriptions = model.Subscriptions.Add (model.Key, inner); Key = model.Key + 1 }
                                else
                                    //printfn "InnerObservable: Queue"
                                    model.Queue.Add xs
                                    return model
                            | Msg.InnerCompleted key ->
                                //printfn "InnerComplete: %A" key
                                let subscriptions = model.Subscriptions.Remove key

                                if model.Queue.Count > 0 then
                                    let xs = model.Queue.[0]
                                    model.Queue.RemoveAt 0
                                    //printfn "InnerComplete: Subscribing: %A" model.Key
                                    let! inner = xs.SubscribeAsync (obv model.Key)

                                    return { model with Subscriptions = subscriptions.Add (model.Key, inner); Key = model.Key + 1 }
                                else if subscriptions.Count > 0 then
                                    return { model with Subscriptions = subscriptions }
                                else
                                    if model.IsStopped then
                                        //printfn "InnerComplete: completing"
                                        do! safeObv.OnCompletedAsync ()
                                    return { model with Subscriptions = Map.empty }
                            | Msg.OuterCompleted ->
                                //printfn "OuterComplete"
                                if model.Subscriptions.Count = 0 then
                                    //printfn "OuterComplete: completing"
                                    do! safeObv.OnCompletedAsync ()
                                return { model with IsStopped = true }
                            | Msg.Dispose ->
                                for KeyValue(key, dispose) in model.Subscriptions do
                                    do! dispose.DisposeAsync ()
                                return initialModel
                        }

                    let rec messageLoop (model : Model<'a>) = async {
                        let! msg = inbox.Receive ()
                        let! newModel = update msg model
                        return! messageLoop newModel
                    }

                    messageLoop initialModel
                )
            async {
                let obv = {
                    new IAsyncObserver<IAsyncObservable<'a>> with
                        member this.OnNextAsync xs = async {
                            Msg.InnerObservable xs |> agent.Post
                        }
                        member this.OnErrorAsync err = async {
                            do! safeObv.OnErrorAsync err
                            agent.Post Msg.Dispose
                        }
                        member this.OnCompletedAsync () = async {
                            Msg.OuterCompleted |> agent.Post
                        }
                    }
                let! dispose = source.SubscribeAsync obv
                let cancel () =
                    async {
                        do! dispose.DisposeAsync ()
                        agent.Post Msg.Dispose
                    }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns an observable sequence that contains the elements of
    /// each given sequences, in sequential order.
    let concatSeq (sources: seq<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        Create.ofSeq(sources)
        |> mergeInner 1

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
