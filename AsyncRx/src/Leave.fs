namespace Reaction.AsyncRx

#if !FABLE_COMPILER
open System.Threading
open FSharp.Control

[<RequireQualifiedAccess>]
module Leave =
    /// Convert async observable to async sequence, non-blocking.
    /// Producer will be awaited until item is consumed by the async
    /// enumerator.
    let toAsyncSeq (source: IAsyncObservable<'a>) : AsyncSeq<'a> =
        let ping = new AutoResetEvent false
        let pong = new AutoResetEvent false
        let mutable latest : Notification<'a> = OnCompleted

        let _obv n =
            async {
                latest <- n
                ping.Set () |> ignore
                do! Async.AwaitWaitHandle pong |> Async.Ignore
            }

        asyncSeq {
            let! dispose = AsyncObserver _obv |> source.SubscribeAsync
            let mutable running = true

            while running do
                do! Async.AwaitWaitHandle ping |> Async.Ignore
                match latest with
                | OnNext x ->
                    yield x
                | OnError ex ->
                    running <- false
                    raise ex
                | OnCompleted ->
                    running <- false
                pong.Set () |> ignore

            do! dispose.DisposeAsync ()
        }
#endif