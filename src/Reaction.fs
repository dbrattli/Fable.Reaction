namespace Fable.Reaction

open System

open Fable
open Fable.Core
open Fable.React

open FSharp.Control

type Dispatch<'Msg> = 'Msg -> unit

[<AutoOpen>]
module Debug =
    #if DEBUG
    let inline debug (text: string, o: #obj) = printfn "%s %A" text o
    #else
    let inline debug (text: string, o: #obj) = ()
    #endif

// fsharplint:disable

type Reaction =

    /// Stateful AsyncRx stream hook.
    static member useStatefulStream (state: 'state, dispatch: Dispatch<'Msg>, stream: 'state -> IAsyncObservable<'Msg> -> TaggedStream<'Msg, 'Tag>) =
        // Create the dispatch and the message stream once.
        let dispatch', msgs =
            Hooks.useMemo(fun () ->
                let obv, obs = AsyncRx.mbSubject<'Msg> ()
                OnNext >> obv.Post,  obs
            , [||])

        // Only re-run the stream function if the state changes.
        let msgs', tag =
            Hooks.useMemo(fun () ->
                let msgs, tag = stream state msgs
                msgs |> AsyncRx.toObservable, tag
            , [| state |])

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


/// Holds React components that needs to be defined at the module level.
module Internal =
    let streamComponent<'Model, 'Msg, 'Tag> =
        FunctionComponent.Of(fun (input: {| initialModel: 'Model; view: 'Model -> Dispatch<'Msg> -> ReactElement; update: 'Model -> 'Msg -> 'Model; stream: 'Model -> IAsyncObservable<'Msg> -> TaggedStream<'Msg, 'Tag> |}) ->
            let model = Hooks.useReducer(input.update, input.initialModel)
            let dispatch, _ = Reaction.useStatefulStream(model.current, model.update, input.stream)

            input.view model.current dispatch
        , "Reaction.streamComponent")

    let streamComponentWithProps<'Model, 'Msg, 'Tag, 'Props> = FunctionComponent.Of(fun (input: {| initialModel: 'Model; view: 'Model -> Dispatch<'Msg> -> ReactElement; update: 'Model -> 'Msg -> 'Model; stream: 'Props ->'Model -> IAsyncObservable<'Msg> -> TaggedStream<'Msg, 'Tag>; props: 'Props |}) ->
        let model = Hooks.useReducer(input.update, input.initialModel)
        let dispatch, _ = Reaction.useStatefulStream(model.current, model.update, input.stream input.props)

        input.view model.current dispatch
    , "Reaction.streamComponent")

type Reaction with
    /// Simple MVU view with stateful stream.
    static member inline streamComponent<'Model, 'Msg, 'Tag>
          (initialModel: 'Model,
          view: 'Model -> Dispatch<'Msg> -> ReactElement,
          update: 'Model -> 'Msg -> 'Model,
          stream: 'Model -> IAsyncObservable<'Msg> -> TaggedStream<'Msg, 'Tag>) =
             Internal.streamComponent {| initialModel = initialModel; view = view; update = update; stream=stream |}

    /// Simple MVU view with stateful stream with props. The props will be given to the stream function in addition to the model.
    static member inline streamComponent<'Model, 'Msg, 'Tag, 'Props>
        (initialModel: 'Model,
        view: 'Model -> Dispatch<'Msg> -> ReactElement,
        update: 'Model -> 'Msg -> 'Model,
        stream: 'Props -> 'Model -> IAsyncObservable<'Msg> -> TaggedStream<'Msg, 'Tag>) =

        fun (props: 'Props) ->
             Internal.streamComponentWithProps {| initialModel = initialModel; view = view; update = update; stream=stream; props=props |}
