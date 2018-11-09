module Test.Reaction.Context

open System
open System.Collections.Concurrent
open System.Threading;
open System.Threading.Tasks

open Reaction

// A single thread test synchronization context.
// Inspired by: https://blogs.msdn.microsoft.com/pfxteam/2012/01/20/await-synchronizationcontext-and-console-apps/
type TestSynchronizationContext () =
    inherit SynchronizationContext ()

    let mutable delayed : List<DateTime*SendOrPostCallback*Object> = List.empty
    let ready = new BlockingCollection<SendOrPostCallback*Object> ()
    let mutable now = DateTime.Now
    let gate = Object ()

    member val private Delayed = delayed with get, set
    member val private Running = false with get, set
    member private this.Ready = ready

    interface IReactionTime with
        member this.Now =
            printfn "Now: %A" now
            now

        member this.SleepAsync (msecs: int) =
            //printfn "SleepAsync: %d" msecs
            let task = new TaskCompletionSource<unit> ()

            let action (_ : Object) =
                task.SetResult ()

            async {
                let timeout = TimeSpan.FromMilliseconds (float msecs)
                let dueTime = now + timeout

                lock gate (fun () ->
                    let workItem = (dueTime, SendOrPostCallback action, null) :: this.Delayed
                    this.Delayed <- List.sortBy (fun (x, _, _) -> x) workItem
                )

                this.ProcessDelayed ()
                return! Async.AwaitTask task.Task
            }

    override this.Post(d : SendOrPostCallback, state: Object) =
        printfn "Post %A" d
        if this.Running then
            this.Ready.Add((d, state))
        else
            d.Invoke state

    /// Process delayed tasks and advances time
    member private this.ProcessDelayed () =
        lock gate (fun () ->
            if this.Ready.Count = 0 then
                match this.Delayed with
                | (x, y, z) :: rest ->
                    //this.Ready.Add ((y, z))
                    this.Post (y, z)
                    this.Delayed <- rest
                    now <- x
                | [] -> ()
            )

    /// Process ready queue
    member private this.ProcessReady() =
        printfn "ProcessReady"
        let workItem = ref (SendOrPostCallback (fun _ -> ()), null)

        this.ProcessDelayed ()

        while (this.Ready.TryTake (workItem, Timeout.Infinite)) do
            printfn "Got workitem: %A" workItem
            let a, b = !workItem
            a.Invoke b

            this.ProcessDelayed ()
        ()

    /// Run async function until completion
    member this.RunAsync(func: Async<unit>) = async {
        let cts = new CancellationTokenSource ()
        let prevCtx = SynchronizationContext.Current

        SynchronizationContext.SetSynchronizationContext this
        do! Async.SwitchToContext this

        this.Running <- true
        ReactionContext.Current <- this

        Async.StartWithContinuations(
            func,
            (fun cont ->
                printfn "cont-%A" cont
                this.Ready.CompleteAdding ()),
            (fun exn ->
                this.Ready.CompleteAdding ()
                printfn "exception-%s" <| exn.ToString()),
            (fun exn ->
                this.Ready.CompleteAdding ()
                printfn "cancel-%s" <| exn.ToString()),
                cts.Token
            )

        try
            this.ProcessReady ()
        with
        | exn -> printfn "Exception-%s" <| exn.ToString ()

        ReactionContext.Reset ()
        this.Running <- false
        SynchronizationContext.SetSynchronizationContext prevCtx
        do! Async.SwitchToContext prevCtx
}