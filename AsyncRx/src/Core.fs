namespace Reaction.AsyncRx

open System
open System.Threading
open Types

module Core =
    let infinite = Seq.initInfinite (fun index -> index)

    let canceller () =
        let cts = new CancellationTokenSource()
        let cancel () = async {
            cts.Cancel ()
        }
        let disposable = { new IAsyncDisposable with member __.DisposeAsync () = cancel () }
        disposable, cts.Token

    /// Safe observer that wraps the given observer and makes sure that
    /// the Rx grammar (onNext* (onError|onCompleted)?) is not violated.
    let safeObserver (obv: IAsyncObserver<'a>) : IAsyncObserver<'a> =
        let agent = MailboxProcessor.Start (fun inbox ->
            let rec messageLoop stopped = async {
                let! n = inbox.Receive()

                if stopped then
                    return! messageLoop stopped

                let! stop = async {
                    match n with
                    | OnNext x ->
                        try
                            do! obv.OnNextAsync x
                            return false
                        with
                        | ex ->
                            do! obv.OnErrorAsync ex
                            return true
                    | OnError ex ->
                        do! obv.OnErrorAsync ex
                        return true
                    | OnCompleted ->
                        do! obv.OnCompletedAsync ()
                        return true
                }

                return! messageLoop stop
            }

            messageLoop false
        )
        { new IAsyncObserver<'a> with
            member this.OnNextAsync x = async {
                OnNext x |> agent.Post
            }
            member this.OnErrorAsync err = async {
                OnError err |> agent.Post
            }
            member this.OnCompletedAsync () = async {
                OnCompleted  |> agent.Post
            }
        }

    let refCountAgent initial action =
        MailboxProcessor.Start(fun inbox ->
            let rec messageLoop count = async {
                let! cmd = inbox.Receive ()
                let newCount =
                    match cmd with
                    | Increase -> count + 1
                    | Decrease -> count - 1

                if newCount = 0 then
                    do! action
                    return ()

                return! messageLoop newCount
            }

            messageLoop initial
        )


[<AutoOpen>]
module Context =
    type IReactionTime =
       abstract member SleepAsync : int -> Async<unit>

       abstract member Now : DateTime

    type ReactionContext () =
        static let instance : IReactionTime = ReactionContext () :> IReactionTime
        static member val Current = instance with get, set

        interface IReactionTime with
            member this.SleepAsync msecs =
                Async.Sleep msecs

            member this.Now =
                DateTime.Now

        static member SleepAsync = ReactionContext.Current.SleepAsync

        static member Now = ReactionContext.Current.Now

        static member Reset () = ReactionContext.Current <- instance