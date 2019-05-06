namespace FSharp.Control

type QueryBuilder () =
    member this.Zero () : IAsyncObservable<_> = Create.empty ()
    member this.Yield (x: 'a) : IAsyncObservable<'a> = Create.single x
    member this.YieldFrom (xs: IAsyncObservable<'a>) : IAsyncObservable<'a> = xs
    member this.Combine (xs: IAsyncObservable<'a>, ys: IAsyncObservable<'a>) =
        Combine.concatSeq [xs; ys]
    member this.Delay (fn) = fn ()
    member this.Bind(source: IAsyncObservable<'a>, fn: 'a -> IAsyncObservable<'b>) : IAsyncObservable<'b> =
        Transformation.flatMap fn source
    member x.For(source: IAsyncObservable<_>, func) : IAsyncObservable<'b> =
        Transformation.concatMap func source

    // Async to AsyncObservable conversion
    member this.Bind (source: Async<'a>, fn: 'a -> IAsyncObservable<'b>) =
        Create.ofAsync source
        |> Transformation.flatMap fn
    member this.YieldFrom (xs: Async<'x>) = Create.ofAsync xs

    // Sequence to AsyncObservable conversion
    member x.For(source: seq<_>, func) : IAsyncObservable<'b> =
        Create.ofSeq source
        |> Transformation.concatMap func

[<AutoOpen>]
module QueryBuilder =
    /// Query builder for an async reactive event source
    let asyncRx = QueryBuilder ()
