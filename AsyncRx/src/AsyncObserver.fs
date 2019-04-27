namespace FSharp.Control

type AsyncObserver<'a> (fn: Notification<'a> -> Async<unit>) =

    interface IAsyncObserver<'a> with
        member this.OnNextAsync (x: 'a) =  OnNext x |> fn
        member this.OnErrorAsync err = OnError err |> fn
        member this.OnCompletedAsync () = OnCompleted |> fn

    static member Create (fn) : IAsyncObserver<'a> =
        AsyncObserver<'a> fn :> IAsyncObserver<'a>
