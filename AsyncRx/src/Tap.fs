namespace Reaction

open Core

[<RequireQualifiedAccess>]
module Tap =
    /// Tap asynchronously into the stream performing side effects by the given async actions.
    let tapAsync (onNextAsync: 'a -> Async<unit>) (onErrorAsync: exn -> Async<unit>) (onCompletedAsync: unit -> Async<unit>)
        (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let subscribeAsync (obvAsync : IAsyncObserver<'a>) =
            async {
                let _obv =
                    { new IAsyncObserver<'a> with
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
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Tap asynchronously into the stream performing side effects by the given `onNextAsync` action.
    let tapOnNextAsync (onNextAsync: 'a -> Async<unit>) : IAsyncObservable<'a> -> IAsyncObservable<'a> =
        tapAsync onNextAsync noopAsync noopAsync

    /// Tap synchronously into the stream performing side effects by the given `onNext` action.
    let tapOnNext (onNext: 'a -> unit) : IAsyncObservable<'a> -> IAsyncObservable<'a> =
        let onNextAsync x = async {
            onNext x
        }
        tapAsync onNextAsync noopAsync noopAsync