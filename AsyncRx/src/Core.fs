namespace Reaction

open System.Threading

module Core =
    let infinite = Seq.initInfinite (fun index -> index)

    let canceller () =
        let cts = new CancellationTokenSource()
        let cancel () = async {
            cts.Cancel ()
        }
        let disposable = { new IAsyncDisposable with member __.DisposeAsync () = cancel () }
        disposable, cts.Token

    /// Safe observer that wraps the given observer. Makes sure that
    /// invocations are serialized and that the Rx grammar (OnNext*
    /// (OnError|OnCompleted)?) is not violated.
    let safeObserver (obv: IAsyncObserver<'a>) : IAsyncObserver<'a> =
        let agent = MailboxProcessor.Start (fun inbox ->
            let rec messageLoop stopped = async {
                let! n = inbox.Receive ()

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
            messageLoop false)
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
