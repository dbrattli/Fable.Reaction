namespace Fable.Reaction

open Fable.Core
open FSharp.Control


[<AutoOpen>]
module QueryExtension =
    let internal ofPromise (pr: Fable.Core.JS.Promise<_>) =
        AsyncRx.ofAsyncWorker(fun obv _ -> async {
            try
                let! result = Async.AwaitPromise pr
                do! obv.OnNextAsync result
                do! obv.OnCompletedAsync ()
            with
            | ex ->
                do! obv.OnErrorAsync ex
        })

    type QueryBuilder with
        // Promise to AsyncObservable conversion
        member this.Bind (source: JS.Promise<'a>, fn: 'a -> IAsyncObservable<'b>) =
            ofPromise source
            |> AsyncRx.flatMap fn

        member this.YieldFrom (xs: JS.Promise<'a>) = ofPromise xs
