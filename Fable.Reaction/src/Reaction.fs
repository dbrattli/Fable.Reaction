namespace Fable.Reaction

open System
open Fable.React
open FSharp.Control

type Dispatch<'Msg> = 'Msg -> unit

[<RequireQualifiedAccess>]
module Reaction =
    #if DEBUG
    let debug (text: string, o: #obj) = printfn "%s %A" text o
    #else
    let debug (text: string, o: #obj) = ()
    #endif

    /// Stateful stream hook.
    let useStatefulStream (state: 'state, dispatch: Dispatch<'msg>, stream: 'state -> IAsyncObservable<'msg> -> TaggedStream<'msg, 'tag>) =
        // Create the dispatch and the message stream once.
        let dispatch', msgs =
            Hooks.useMemo((fun () ->
                let obv, obs = AsyncRx.mbSubject<'msg> ()
                OnNext >> obv.Post,  obs
            ), Array.empty)

        // Only re-run the stream function if the state changes.
        let msgs', tag =
            Hooks.useMemo((fun () ->
                let msgs, tag = stream state msgs
                msgs |> AsyncRx.toObservable, tag
            ), [| state |])

        // Re-subscribe the stream if the tag has changed.
        Hooks.useEffectDisposable(fun () ->
            let disposable =
                debug ("[Fable.Reaction] Stream subscribed: ", tag)
                msgs'.Subscribe(function
                    | OnNext msg -> dispatch msg
                    | OnCompleted -> debug ("[Fable.Reaction] Stream completed:", tag)
                    | OnError err -> debug ("[Fable.Reaction] Stream error: ", (err.ToString ()))
                )

            { new IDisposable with
                member __.Dispose () =
                        debug ("[Fable.Reaction] Stream disposed: ", tag)
                        disposable.Dispose () }
        , [| tag |])

        dispatch', msgs'

    /// Simple MVU view.
    let View<'model, 'msg> (initialModel: 'model)
              (view: 'model -> Dispatch<'msg> -> ReactElement)
              (update: 'model -> 'msg -> 'model) =

        FunctionComponent.Of(fun () ->
            let model = Hooks.useReducer(update, initialModel)
            view model.current model.update
        , "Reaction.View")

    /// Simple MVU view with stateful stream.
    let StreamView<'model, 'msg, 'tag> (initialModel: 'model)
              (view: 'model -> Dispatch<'msg> -> ReactElement)
              (update: 'model -> 'msg -> 'model)
              (stream: 'model -> IAsyncObservable<'msg> -> TaggedStream<'msg, 'tag>) =

        FunctionComponent.Of(fun () ->
            let model = Hooks.useReducer(update, initialModel)
            let dispatch, _ = useStatefulStream(model.current, model.update, stream)

            view model.current dispatch
        , "Reaction.StreamView")

    let StreamViewWithProps<'model, 'msg, 'tag, 'props> (initialModel: 'model)
              (view: 'props -> 'model -> Dispatch<'msg> -> ReactElement)
              (update: 'model -> 'msg -> 'model)
              (stream: 'props ->'model -> IAsyncObservable<'msg> -> TaggedStream<'msg, 'tag>) =

        FunctionComponent.Of(fun (props : 'props) ->
            let model = Hooks.useReducer(update, initialModel)
            let dispatch, _ = useStatefulStream(model.current, model.update, stream props)

            view props model.current dispatch
        , "Reaction.StreamViewWithProps")
