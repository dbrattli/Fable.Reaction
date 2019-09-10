namespace FSharp.Control

open System
open Core

/// Overloads and extensions for AsyncDisposable
type AsyncDisposable (cancel) =
    interface IAsyncDisposable with
        member this.DisposeAsync () =
            async {
                do! cancel ()
            }

    static member Create (cancel) : IAsyncDisposable =
        AsyncDisposable cancel :> IAsyncDisposable

    static member Empty : IAsyncDisposable =
        let cancel () = async {
            return ()
        }
        AsyncDisposable cancel :> IAsyncDisposable

    static member Composite (disposables: IAsyncDisposable seq) : IAsyncDisposable =
        let cancel () = async {
            for d in disposables do
                do! d.DisposeAsync ()
        }
        AsyncDisposable (cancel) :> IAsyncDisposable

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
    type IAsyncDisposable with
        member this.ToDisposable () =
            { new IDisposable with member __.Dispose () = this.DisposeAsync () |> Async.Start' }

    type IDisposable with
        member this.ToAsyncDisposable () : IAsyncDisposable =
            { new IAsyncDisposable with member __.DisposeAsync () = async { this.Dispose () } }
