namespace Fable.Reaction

open Fable.Core
open Fable.Import.Browser

open Reaction

[<AutoOpen>]
module ReactionExtension =
    type INamedAsyncObservable<'a> =
        inherit IAsyncObservable<'a>

        abstract member Name : string

    let named (name: string) (source: IAsyncObservable<'a>) : INamedAsyncObservable<'a> =
        { new INamedAsyncObservable<'a> with
            member __.SubscribeAsync o = source.SubscribeAsync o
            member __.Name = name
        }

    /// Returns an observable that produces a notification when the
    /// promise resolves. The observable will also complete after
    /// producing an event.
    let ofPromise (pr: Fable.Import.JS.Promise<_>) =
        Create.ofAsyncWorker(fun obv _ -> async {
            try
                let! result = Async.AwaitPromise pr
                do! obv.OnNextAsync result
                do! obv.OnCompletedAsync ()
            with
            | ex ->
                do! obv.OnErrorAsync ex
        })

    /// Returns an async observable of mouse events.
    let ofMouseMove () : IAsyncObservable<Fable.Import.Browser.MouseEvent> =
        let subscribe (obv: IAsyncObserver<Fable.Import.Browser.MouseEvent>) : Async<IAsyncDisposable> =
            async {
                let onMouseMove (ev: Fable.Import.Browser.MouseEvent) =
                    async {
                        do! obv.OnNextAsync ev
                    } |> Async.StartImmediate

                window.addEventListener_mousemove onMouseMove
                let cancel () = async {
                    window.removeEventListener ("mousemove", unbox onMouseMove)
                }
                return AsyncDisposable.Create cancel
            }

        AsyncObservable.create subscribe

    /// Deprecated. Use ofMouseMove instead.
    let fromMouseMoves = ofMouseMove
