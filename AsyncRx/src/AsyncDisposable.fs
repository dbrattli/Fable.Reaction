namespace FSharp.Control

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

