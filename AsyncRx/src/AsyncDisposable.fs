namespace FSharp.Control

open System
open System.Threading

open FSharp.Control.Core

/// Overloads and extensions for AsyncDisposable
type AsyncDisposable private (cancel) =
    let mutable isDisposed = 0
    interface IAsyncRxDisposable with
        member this.DisposeAsync () = async {
#if FABLE_COMPILER
            if isDisposed = 0 then
                isDisposed <- 1
                do! cancel ()
#else
            if System.Threading.Interlocked.Exchange(&isDisposed, 1) = 0 then
                do! cancel ()
#endif
        }

    static member Create cancel : IAsyncRxDisposable =
        AsyncDisposable cancel :> IAsyncRxDisposable

    static member Empty : IAsyncRxDisposable =
        let cancel () = async {
            return ()
        }
        AsyncDisposable cancel :> IAsyncRxDisposable

    static member Composite (disposables: IAsyncRxDisposable seq) : IAsyncRxDisposable =
        let cancel () = async {
            for d in disposables do
                do! d.DisposeAsync ()
        }
        AsyncDisposable cancel :> IAsyncRxDisposable

type Disposable (cancel) =
    let mutable isDisposed = 0
    interface IDisposable with
        member this.Dispose () =
#if FABLE_COMPILER
            if isDisposed = 0 then
                isDisposed <- 1
                cancel ()
#else
            if System.Threading.Interlocked.Exchange(&isDisposed, 1) = 0 then
                cancel ()
#endif

    static member Create (cancel) : IDisposable =
        new Disposable(cancel) :> IDisposable

    static member Empty : IDisposable =
        let cancel () =
            ()

        new Disposable(cancel) :> IDisposable

    static member Composite (disposables: IDisposable seq) : IDisposable =
        let cancel () =
            for d in disposables do
                d.Dispose ()

        new Disposable (cancel) :> IDisposable

[<AutoOpen>]
module AsyncDisposable =
    type IAsyncRxDisposable with
        member this.ToDisposable () =
            { new IDisposable with member __.Dispose () = this.DisposeAsync () |> Async.Start' }

    type System.IDisposable with
        member this.ToAsyncDisposable () : IAsyncRxDisposable =
            AsyncDisposable.Create (fun () -> async { this.Dispose () })

    let canceller () =
        let cts = new CancellationTokenSource()
        let cancel () = async {
            cts.Cancel ()
        }
        AsyncDisposable.Create cancel, cts.Token

