namespace Fable.Reaction

open Fable.Core
open Fable.Import.Browser

open Reaction

[<AutoOpen>]
module Fable =

    let ofPromise (pr: Fable.Import.JS.Promise<_>) =
        let obv = Creation.ofAsync(fun obv _ -> async {
            try
                let! result = Async.AwaitPromise pr
                do! OnNext result |> obv
                do! OnCompleted |> obv
            with
            | ex ->
                do! OnError ex |> obv
        })
        AsyncObservable obv

    let fromMouseMoves () : AsyncObservable<MouseEvent> =
        let subscribe (obv : Types.AsyncObserver<MouseEvent>) : Async<Types.AsyncDisposable> =
            async {
                let onMouseMove (ev : Fable.Import.Browser.MouseEvent) =
                    async {
                        do! OnNext ev |> obv
                    } |> Async.StartImmediate

                window.addEventListener_mousemove onMouseMove
                let cancel () = async {
                    window.removeEventListener ("mousemove", unbox onMouseMove)
                }
                return cancel
            }

        AsyncObservable subscribe
