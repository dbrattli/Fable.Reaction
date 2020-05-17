namespace FSharp.Control
open System.Threading

open FSharp.Control.Core

type QueryBuilder () =
    member this.Zero () : IAsyncObservable<_> = Create.empty ()
    member this.Yield (x: 'TSource) : IAsyncObservable<'TSource> = Create.single x
    member this.YieldFrom (xs: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> = xs
    member this.Combine (xs: IAsyncObservable<'TSource>, ys: IAsyncObservable<'TSource>) =
        Combine.concatSeq [xs; ys]
    member this.Delay (fn) = fn ()
    member this.Bind(source: IAsyncObservable<'TSource>, fn: 'TSource -> IAsyncObservable<'TResult>) : IAsyncObservable<'TResult> =
        Transform.flatMap fn source
    member x.For(source: IAsyncObservable<_>, func: 'TSource -> IAsyncObservable<'TResult>) : IAsyncObservable<'TResult> =
        Transform.concatMap func source

    // Async to AsyncObservable conversion
    member this.Bind (source: Async<'TSource>, fn: 'TSource -> IAsyncObservable<'TResult>) =
        Create.ofAsync source
        |> Transform.flatMap fn
    member this.YieldFrom (xs: Async<'x>) = Create.ofAsync xs

    // Sequence to AsyncObservable conversion
    member x.For(source: seq<'TSource>, func: 'TSource -> IAsyncObservable<'TResult>) : IAsyncObservable<'TResult> =
        Create.ofSeq source
        |> Transform.concatMap func

[<AutoOpen>]
module QueryBuilder =
    /// Query builder for an async reactive event source
    let asyncRx = QueryBuilder ()

    /// We extend AsyncBuilder to use `use!` for resource managemnt when using async builder.
    type AsyncBuilder with
        member builder.Using(resource:#IAsyncRxDisposable, f: #IAsyncRxDisposable -> Async<'TSource>) =
            let mutable x = 0
            let disposeFunction _ =
#if !FABLE_COMPILER
                if Interlocked.CompareExchange(&x, 1, 0) = 0 then
#endif
                    resource.DisposeAsync()
                    |> Async.Start' // Dispose is best effort.

            async.TryFinally(f resource, disposeFunction)

