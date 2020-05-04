namespace FSharp.Control

open System
open Core

/// Overloads and extensions for AsyncDisposable
type AsyncDisposable (cancel) =
    interface FSharp.Control.IAsyncDisposable with
        member this.DisposeAsync () =
            cancel ()

    static member Create (cancel) : FSharp.Control.IAsyncDisposable =
        AsyncDisposable cancel :> FSharp.Control.IAsyncDisposable

    static member Empty : FSharp.Control.IAsyncDisposable =
        let cancel () = async {
            return ()
        }
        AsyncDisposable cancel :> FSharp.Control.IAsyncDisposable

    static member Composite (disposables: FSharp.Control.IAsyncDisposable seq) : FSharp.Control.IAsyncDisposable =
        let cancel () = async {
            for d in disposables do
                do! d.DisposeAsync ()
        }
        AsyncDisposable cancel :> FSharp.Control.IAsyncDisposable

type Disposable (cancel) =
    interface IDisposable with
        member this.Dispose () =
            cancel ()

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
    type FSharp.Control.IAsyncDisposable with
        member this.ToDisposable () =
            { new IDisposable with member __.Dispose () = this.DisposeAsync () |> Async.Start' }

    type System.IDisposable with
        member this.ToAsyncDisposable () : FSharp.Control.IAsyncDisposable =
            { new FSharp.Control.IAsyncDisposable with member __.DisposeAsync () = async { this.Dispose () } }
