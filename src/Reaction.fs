namespace Fable.Reaction

open Fable.Core
open Fable.Import.Browser

open Reaction

[<AutoOpen>]
module ReactionExtension =

    /// Returns an observable that produces a notification when the
    /// promise resolves. The observable will also complete after
    /// producing an event.
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

    /// Returns an async observable of mouse events.
    let ofMouseMove () : AsyncObservable<MouseEvent> =
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

    /// Deprecated. Use ofMouseMove instead.
    let fromMouseMoves = ofMouseMove