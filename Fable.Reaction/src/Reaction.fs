namespace Fable.Reaction

open System
open Browser
open Fable.React
open FSharp.Control

type Dispatch<'msg> = 'msg -> unit

[<RequireQualifiedAccess>]
module Reaction =
    #if DEBUG
    let debug (text: string, o: #obj) = printfn "%s %A" text o
    #else
    let debug (text: string, o: #obj) = ()
    #endif

    /// Stream hook.
    let useStream (dispatch: 'msg -> unit, stream: IAsyncObservable<'msg> -> TaggedObservable<'msg, 'tag>) =
        let obv, obs = AsyncRx.mbStream<'msg> ()
        let subject = Hooks.useState((OnNext >> obv.Post,  obs))

        let dispatch', msgs = subject.current
        let msgs', tag = stream msgs

        Hooks.useEffectDisposable (fun () ->
            let mutable subscription : IAsyncDisposable = AsyncDisposable.Empty
            async {
                let! disposable =
                    debug ("[Fable.Reaction] Stream subscribed: ", tag)
                    msgs'.SubscribeAsync (fun x -> async {
                        match x with
                        | OnNext msg -> dispatch msg
                        | OnCompleted ->
                            debug ("[Fable.Reaction] Stream completed:", tag)
                        | OnError err ->
                            debug("[Fable.Reaction] Stream error: ", (err.ToString ()))
                            do! subscription.DisposeAsync ()
                    })
                subscription <- disposable
            } |> Async.StartImmediate

            { new IDisposable with
                member __.Dispose () =
                    debug ("[Fable.Reaction] Stream disposed: ", tag)
                    subscription.DisposeAsync () |> Async.StartImmediate
            }
        , [| tag |])

        dispatch', msgs'

    /// Stateful stream hook.
    let useStatefulStream (state: 'state, update: 'msg -> unit, stream: 'state -> IAsyncObservable<'msg> -> TaggedObservable<'msg, 'tag>) =
        useStream (update, stream state)

    /// Simple MVU app component.
    let Component<'model, 'msg> (initialModel: 'model)
              (view: 'model -> ('msg -> unit) -> ReactElement)
              (update: 'model -> 'msg -> 'model) =

        FunctionComponent.Of(fun () ->
            let model = Hooks.useReducer(update, initialModel)
            view model.current model.update
        )

    /// Simple MVU app with stateful stream.
    let StreamComponent<'model, 'msg, 'tag> (initialModel: 'model)
              (view: 'model -> ('msg -> unit) -> ReactElement)
              (update: 'model -> 'msg -> 'model)
              (stream: 'model -> IAsyncObservable<'msg> -> TaggedObservable<'msg, 'tag>) =

        FunctionComponent.Of(fun () ->
            let model = Hooks.useReducer(update, initialModel)
            let dispatch, _ = useStatefulStream(model.current, model.update, stream)

            view model.current dispatch
        )