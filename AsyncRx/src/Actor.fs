namespace FSharp.Control

open FSharp.Control.Core

[<RequireQualifiedAccess>]
module internal Actor =


    /// **Description**
    ///
    /// **Parameters**
    ///   * `init` - parameter of type `unit -> 'model`
    ///   * `update` - parameter of type `'model -> 'msg -> 'model`
    ///   * `view` - parameter of type `'model -> IAsyncObserver<'view> -> unit`
    ///   * `start` - parameter of type `MailboxProcessor<'msg> -> IAsyncDisposable`
    ///
    /// **Output Type**
    ///   * `IAsyncObservable<'view>`
    ///
    /// **Exceptions**
    ///
    let mvu<'model, 'msgIn, 'msgOut> (init: unit -> 'model) (update: 'model -> 'msgIn -> 'model) (view: 'model -> IAsyncObserver<'msgOut> -> Async<unit>) (start: MailboxProcessor<'msgIn> -> Async<IAsyncDisposable>) =
        let subscribeAsync (dispatch: IAsyncObserver<'msgOut>) = async {
            let safeObserver = safeObserver dispatch

            let agent = MailboxProcessor.Start (fun inbox ->
                let rec messageLoop (model : 'model) = async {
                    let! msg = inbox.Receive ()

                    let model' = update model msg
                    do! view model' safeObserver

                    return! messageLoop model'
                }

                let initialModel = init ()
                do view initialModel safeObserver |> Async.Start

                messageLoop initialModel)

            return! start agent
        }

        { new IAsyncObservable<'msgOut> with member __.SubscribeAsync o = subscribeAsync o }

    let operator (source: IAsyncObservable<'msg>) : IAsyncObservable<'view> =
        let init () =
            42

        let update model msg =
            model

        let view model dispatch = async {
            ()
        }

        mvu init update view (fun agent -> async {
            return AsyncDisposable.Empty
        })

