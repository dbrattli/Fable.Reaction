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
            ), [||]) // Note to self: cannot be Array.empty

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
    let private view'<'model, 'msg> = FunctionComponent.Of(fun (input: {| initialModel: 'model; view: 'model -> Dispatch<'msg> -> ReactElement; update: 'model -> 'msg -> 'model |}) ->
        let model = Hooks.useReducer(input.update, input.initialModel)

        input.view model.current model.update
    , "Reaction.View")

    let View<'model, 'msg> (initialModel: 'model)
              (view: 'model -> Dispatch<'msg> -> ReactElement)
              (update: 'model -> 'msg -> 'model) =

        view' {| initialModel = initialModel; view = view; update = update |}

    /// Simple MVU view with stateful stream.
    let private streamView'<'model, 'msg, 'tag> = FunctionComponent.Of(fun (input: {| initialModel: 'model; view: 'model -> Dispatch<'msg> -> ReactElement; update: 'model -> 'msg -> 'model; stream: 'model -> IAsyncObservable<'msg> -> TaggedStream<'msg, 'tag> |}) ->
        let model = Hooks.useReducer(input.update, input.initialModel)
        let dispatch, _ = useStatefulStream(model.current, model.update, input.stream)

        input.view model.current dispatch
    , "Reaction.StreamView")

    let StreamView<'model, 'msg, 'tag>
          (initialModel: 'model)
          (view: 'model -> Dispatch<'msg> -> ReactElement)
          (update: 'model -> 'msg -> 'model)
          (stream: 'model -> IAsyncObservable<'msg> -> TaggedStream<'msg, 'tag>) =
             streamView' {| initialModel = initialModel; view = view; update = update; stream=stream |}

    let private streamViewWithProps'<'model, 'msg, 'tag, 'props> = FunctionComponent.Of(fun (input: {| initialModel: 'model; view: 'model -> Dispatch<'msg> -> ReactElement; update: 'model -> 'msg -> 'model; stream: 'props ->'model -> IAsyncObservable<'msg> -> TaggedStream<'msg, 'tag>; props: 'props |}) ->
        let model = Hooks.useReducer(input.update, input.initialModel)
        let dispatch, _ = useStatefulStream(model.current, model.update, input.stream input.props)

        input.view model.current dispatch
    , "Reaction.StreamView")

    let StreamViewWithProps<'model, 'msg, 'tag, 'props>
          (initialModel: 'model)
          (view: 'model -> Dispatch<'msg> -> ReactElement)
          (update: 'model -> 'msg -> 'model)
          (stream: 'props -> 'model -> IAsyncObservable<'msg> -> TaggedStream<'msg, 'tag>)
          (props: 'props) =
             streamViewWithProps' {| initialModel = initialModel; view = view; update = update; stream=stream; props=props |}
