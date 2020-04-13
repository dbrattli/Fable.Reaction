namespace FSharp.Control

open Core

[<RequireQualifiedAccess>]
module internal Tap =
    /// Tap asynchronously into the stream performing side effects by the given async actions.
    let tapAsync (onNextAsync: 'TSource -> Async<unit>) (onErrorAsync: exn -> Async<unit>) (onCompletedAsync: unit -> Async<unit>)
        (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> =
        let subscribeAsync (obvAsync : IAsyncObserver<'TSource>) =
            async {
                let _obv =
                    { new IAsyncObserver<'TSource> with
                        member this.OnNextAsync x = async {
                            // Let exceptions bubble to the top
                            do! onNextAsync x
                            do! obvAsync.OnNextAsync x
                        }
                        member this.OnErrorAsync err = async {
                            do! onErrorAsync err
                            do! obvAsync.OnErrorAsync err
                        }
                        member this.OnCompletedAsync () = async {
                            do! onCompletedAsync ()
                            do! obvAsync.OnCompletedAsync ()
                        }
                    }
                return! source.SubscribeAsync _obv
            }
        { new IAsyncObservable<'TSource> with member __.SubscribeAsync o = subscribeAsync o }

    /// Tap asynchronously into the stream performing side effects by the given `onNextAsync` action.
    let tapOnNextAsync (onNextAsync: 'TSource -> Async<unit>) : Stream<'TSource> =
        tapAsync onNextAsync noopAsync noopAsync

    /// Tap synchronously into the stream performing side effects by the given `onNext` action.
    let tapOnNext (onNext: 'TSource -> unit) : Stream<'TSource> =
        let onNextAsync x = async {
            onNext x
        }
        tapAsync onNextAsync noopAsync noopAsync