namespace FSharp.Control

open System
open FSharp.Control.Core

type AsyncObserver<'T> (fn: Notification<'T> -> Async<unit>) =

    interface IAsyncObserver<'T> with
        member this.OnNextAsync (x: 'T) = OnNext x |> fn
        member this.OnErrorAsync err = OnError err |> fn
        member this.OnCompletedAsync () = OnCompleted |> fn

    static member Create (fn) : IAsyncObserver<'T> =
        AsyncObserver<'T> fn :> IAsyncObserver<'T>

type Observer<'T> (fn: Notification<'T> -> unit) =
    interface IObserver<'T> with
        member this.OnNext (x: 'T) = OnNext x |> fn
        member this.OnError err = OnError err |> fn
        member this.OnCompleted () = OnCompleted |> fn

    static member Create (fn) : IObserver<'T> =
        Observer<'T> fn :> IObserver<'T>

[<AutoOpen>]
module AsyncObserver =
    type IAsyncObserver<'T> with
        /// Convert async observer (IAsyncObserver) to an observer (IObserver).
        member this.ToObserver () =
            { new IObserver<'T> with
                member __.OnNext x = this.OnNextAsync x |> Async.Start'
                member __.OnError err = this.OnErrorAsync err |> Async.Start'
                member __.OnCompleted () = this.OnCompletedAsync () |> Async.Start'
            }

    type IObserver<'T> with
        /// Convert observer (IObserver) to an async observer (IAsyncObserver).
        member this.ToAsyncObserver () =
            { new IAsyncObserver<'T> with
                member __.OnNextAsync x = async { this.OnNext x }
                member __.OnErrorAsync err = async { this.OnError err }
                member __.OnCompletedAsync () = async { this.OnCompleted () }
            }

