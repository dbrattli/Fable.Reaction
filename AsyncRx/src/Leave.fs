namespace FSharp.Control

#if !FABLE_COMPILER
open System.Threading

[<RequireQualifiedAccess>]
module internal Leave =
    /// Convert async observable to async sequence, non-blocking. Producer will be awaited until item is consumed by the
    /// async enumerator.
    let toAsyncSeq (source: IAsyncObservable<'TSource>): AsyncSeq<'TSource> =
        let ping = new AutoResetEvent false
        let pong = new AutoResetEvent false
        let mutable latest: Notification<'TSource> = OnCompleted

        let _obv n =
            async {
                latest <- n
                ping.Set() |> ignore
                do! Async.AwaitWaitHandle pong |> Async.Ignore
            }

        asyncSeq {
            let! dispose = AsyncObserver _obv |> source.SubscribeAsync
            let mutable running = true

            while running do
                do! Async.AwaitWaitHandle ping |> Async.Ignore
                match latest with
                | OnNext x -> yield x
                | OnError ex ->
                    running <- false
                    raise ex
                | OnCompleted -> running <- false
                pong.Set() |> ignore

            do! dispose.DisposeAsync()
        }
#endif
