module Tests.Utils

open System
open System.Collections.Generic
open System.Threading.Tasks

open Reaction

type TestObserver<'a>() =
    let notifications = new List<Notification<'a>>()
    let completed = TaskCompletionSource<'a>()
    let monitor = new Object ()
    let mutable latest : 'a option = None

    member this.Notifications = notifications

    interface IAsyncObserver<'a> with
        member this.OnNextAsync x = async {
            do! this.PostAsync (OnNext x)
        }
        member this.OnErrorAsync err = async {
            do! this.PostAsync (OnError err)
        }
        member this.OnCompletedAsync () = async {
            do! this.PostAsync OnCompleted
        }

    member this.PostAsync (n : Notification<'a>) =
        async {
            //printfn "TestObserver %A" n

            lock monitor (fun () ->
                this.Notifications.Add(n)
            )

            match n with
            | OnNext x -> latest <- Some x
            | OnError e -> completed.SetException e
            | OnCompleted ->
                match latest with
                | Some x -> completed.SetResult x
                | None -> completed.SetCanceled ()
        }
    member this.Await () : Async<'a> =
        async {
            return! Async.AwaitTask completed.Task
        }

    member this.AwaitIgnore () : Async<unit> =
        async {
            try
                do! Async.AwaitTask completed.Task |> Async.Ignore
            with
            | :? TaskCanceledException -> ()
        }

let fromNotification (notifications : seq<Notification<'a>>) =
    Create.ofAsyncWorker (fun obv token -> async {
        for notification in notifications do
            if token.IsCancellationRequested then
                raise (OperationCanceledException("Operation cancelled"))

            match notification with
            | OnNext x ->
                try
                    do! obv.OnNextAsync x
                with err ->
                    do! obv.OnErrorAsync err
            | OnError err ->
                do! obv.OnErrorAsync err
            | OnCompleted ->
                do! obv.OnCompletedAsync ()
    })

