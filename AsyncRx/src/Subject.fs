namespace FSharp.Control

open System.Collections.Generic
open System.Threading

open Core

module Subjects =
    /// A cold stream that only supports a single subscriber
    let singleSubject<'TSource> () : IAsyncObserver<'TSource> * IAsyncObservable<'TSource> =
        let mutable oobv: IAsyncObserver<'TSource> option = None
        let cts = new CancellationTokenSource ()

        let subscribeAsync (aobv : IAsyncObserver<'TSource>) : Async<IAsyncRxDisposable> =
            let sobv = safeObserver aobv
            if Option.isSome oobv then
                failwith "singleStream: Already subscribed"

            oobv <- Some sobv
            cts.Cancel ()

            async {
                let cancel () = async {
                    oobv <- None
                }
                return AsyncDisposable.Create cancel
            }

        let obv (n: Notification<'TSource>) =
            async {
                while oobv.IsNone do
                    // Wait for subscriber
                    Async.StartImmediate (Async.Sleep 100, cts.Token)

                match oobv with
                | Some obv ->
                    match n with
                    | OnNext x ->
                        try
                            do! obv.OnNextAsync x
                        with ex ->
                            do! obv.OnErrorAsync ex
                    | OnError e -> do! obv.OnErrorAsync e
                    | OnCompleted -> do! obv.OnCompletedAsync ()
                | None ->
                    printfn "No observer for %A" n
                    ()
            }
        let obs = { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }
        AsyncObserver obv :> IAsyncObserver<'TSource>, obs

    /// A mailbox subject is a subscribable mailbox. Each message is broadcasted to all subscribed observers.
    let mbSubject<'TSource> () : MailboxProcessor<Notification<'TSource>>*IAsyncObservable<'TSource> =
        let obvs = new List<IAsyncObserver<'TSource>>()
        let cts = new CancellationTokenSource()

        let mb = MailboxProcessor.Start(fun inbox ->
            let rec messageLoop _ = async {
                let! n = inbox.Receive ()

                for aobv in obvs do
                    match n with
                    | OnNext x ->
                        try
                            do! aobv.OnNextAsync x
                        with ex ->
                            do! aobv.OnErrorAsync ex
                            cts.Cancel ()
                    | OnError err ->
                        do! aobv.OnErrorAsync err
                        cts.Cancel ()
                    | OnCompleted ->
                        do! aobv.OnCompletedAsync ()
                        cts.Cancel ()

                return! messageLoop ()
            }
            messageLoop ()
        , cts.Token)

        let subscribeAsync (aobv: IAsyncObserver<'TSource>) : Async<IAsyncRxDisposable> =
            async {
                let sobv = safeObserver aobv
                obvs.Add sobv

                let cancel () = async {
                    obvs.Remove sobv |> ignore
                }
                return AsyncDisposable.Create cancel
            }

        mb, { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }

    /// A stream is both an observable sequence as well as an observer.
    /// Each notification is broadcasted to all subscribed observers.
    let subject<'TSource> () : IAsyncObserver<'TSource> * IAsyncObservable<'TSource> =
        let mb, obs = mbSubject<'TSource> ()

        let obv = { new IAsyncObserver<'TSource> with
            member this.OnNextAsync x = async {
                OnNext x |> mb.Post
            }
            member this.OnErrorAsync err = async {
                OnError err |> mb.Post
            }
            member this.OnCompletedAsync () = async {
                OnCompleted |> mb.Post
            }
        }

        obv, obs