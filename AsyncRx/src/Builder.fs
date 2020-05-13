namespace FSharp.Control
open System.Threading

open FSharp.Control.Core

type QueryBuilder () =
    member this.Zero () : IAsyncObservable<_> = Create.empty ()
    member this.Yield (x: 'a) : IAsyncObservable<'a> = Create.single x
    member this.YieldFrom (xs: IAsyncObservable<'a>) : IAsyncObservable<'a> = xs
    member this.Combine (xs: IAsyncObservable<'a>, ys: IAsyncObservable<'a>) =
        Combine.concatSeq [xs; ys]
    member this.Delay (fn) = fn ()
    member this.Bind(source: IAsyncObservable<'a>, fn: 'a -> IAsyncObservable<'b>) : IAsyncObservable<'b> =
        Transform.flatMap fn source
    member x.For(source: IAsyncObservable<_>, func: 'a -> IAsyncObservable<'b>) : IAsyncObservable<'b> =
        Transform.concatMap func source

    // Async to AsyncObservable conversion
    member this.Bind (source: Async<'a>, fn: 'a -> IAsyncObservable<'b>) =
        Create.ofAsync source
        |> Transform.flatMap fn
    member this.YieldFrom (xs: Async<'x>) = Create.ofAsync xs

    // Sequence to AsyncObservable conversion
    member x.For(source: seq<'a>, func: 'a -> IAsyncObservable<'b>) : IAsyncObservable<'b> =
        Create.ofSeq source
        |> Transform.concatMap func

[<AutoOpen>]
module QueryBuilder =
    /// Query builder for an async reactive event source
    let asyncRx = QueryBuilder ()

    /// We extend AsyncBuilder to use `use!` for resource managemnt when using async builder.
    type AsyncBuilder with
        member builder.Using(resource:#IAsyncRxDisposable, f: #IAsyncRxDisposable -> Async<'a>) =
            let mutable x = 0
            let disposeFunction _ =
#if !FABLE_COMPILER
                if Interlocked.CompareExchange(&x, 1, 0) = 0 then
#endif
                    resource.DisposeAsync()
                    |> Async.Start' // Dispose is best effort.

            async.TryFinally(f resource, disposeFunction)

