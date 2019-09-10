namespace FSharp.Control

open System
open FSharp.Control.Core

type AsyncObserver<'a> (fn: Notification<'a> -> Async<unit>) =

    interface IAsyncObserver<'a> with
        member this.OnNextAsync (x: 'a) =  OnNext x |> fn
        member this.OnErrorAsync err = OnError err |> fn
        member this.OnCompletedAsync () = OnCompleted |> fn

    static member Create (fn) : IAsyncObserver<'a> =
        AsyncObserver<'a> fn :> IAsyncObserver<'a>

type Observer<'a> (fn: Notification<'a> -> unit) =
    interface IObserver<'a> with
        member this.OnNext (x: 'a) =  OnNext x |> fn
        member this.OnError err = OnError err |> fn
        member this.OnCompleted () = OnCompleted |> fn

    static member Create (fn) : IObserver<'a> =
        Observer<'a> fn :> IObserver<'a>

[<AutoOpen>]
module AsyncObserver =
    type IAsyncObserver<'a> with
        /// Convert async observer (IAsyncObserver) to an observer (IObserver).
        member this.ToObserver () =
            { new IObserver<'a> with
                member __.OnNext x = this.OnNextAsync x |> Async.Start'
                member __.OnError err = this.OnErrorAsync err |> Async.Start'
                member __.OnCompleted () = this.OnCompletedAsync () |> Async.Start'
            }

    type IObserver<'a> with
        /// Convert observer (IObserver) to an async observer (IAsyncObserver).
        member this.ToAsyncObserver () =
            { new IAsyncObserver<'a> with
                member __.OnNextAsync x = async { this.OnNext x }
                member __.OnErrorAsync err = async { this.OnError err }
                member __.OnCompletedAsync () = async { this.OnCompleted () }
            }

