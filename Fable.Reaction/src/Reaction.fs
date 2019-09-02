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

    let renderReact placeholderId =
        let scanner (lastRequest : float option) view =
            match lastRequest with
            | Some r -> window.cancelAnimationFrame r
            | _ -> ()

            Some (window.requestAnimationFrame (fun _ ->
                ReactDom.render(
                    view,
                    document.getElementById(placeholderId)
                )))

        scanner

    /// Stream hook.
    let useStream (update: 'msg -> unit, stream: IAsyncObservable<'msg> -> TaggedObservable<'msg, 'tag>) =
        let obv, obs = AsyncRx.mbStream<'msg> ()
        let subject = Hooks.useState((OnNext >> obv.Post,  obs))

        let dispatch, msgs = subject.current
        let msgs', tag = stream msgs

        Hooks.useEffectDisposable (fun () ->
            let mutable subscription : IAsyncDisposable = AsyncDisposable.Empty
            async {
                let! disposable =
                    msgs'.SubscribeAsync (fun x -> async {
                        match x with
                        | OnNext msg -> update msg
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
                    subscription.DisposeAsync () |> Async.StartImmediate
            }
        , [| tag |])

        dispatch, msgs'

    /// Stateful stream hook.
    let useStatefulStream (state: 'state, update: 'msg -> unit, stream: 'state -> IAsyncObservable<'msg> -> TaggedObservable<'msg, 'tag>) =
        useStream (update, stream state)

    /// Simple MVU app component.
    let mvuApp<'model, 'msg>
            (init: unit -> 'model)
            (view: 'model -> ('msg -> unit) -> ReactElement)
            (update: 'model -> 'msg -> 'model) =

        let initialModel = init ()
        FunctionComponent.Of(fun () ->
            let model = Hooks.useReducer(update, initialModel)
            view model.current model.update
        )

    /// Simple MVU app with stateful stream.
    let mvuStreamApp<'model, 'msg, 'tag>
            (init: unit -> 'model)
            (view: 'model -> ('msg -> unit) -> ReactElement)
            (update: 'model -> 'msg -> 'model)
            (stream: 'model -> IAsyncObservable<'msg> -> TaggedObservable<'msg, 'tag>) =

        let initialModel = init ()
        FunctionComponent.Of(fun () ->
            let model = Hooks.useReducer(update, initialModel)
            let dispatch, _ = useStatefulStream(model.current, model.update, stream)
            view model.current dispatch
        )