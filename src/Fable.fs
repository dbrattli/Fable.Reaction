// TODO: move to a separate project (assembly)

namespace Fable.Reaction

open Fable.Import.Browser

open Reaction

[<AutoOpen>]
module Fable =
    let fromMouseMoves () : AsyncObservable<MouseEvent> =
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

    open Fable.Import.React

    /// Setup rendering of root React component inside html element identified by placeholderId
    let renderReact placeholderId =
        let render view =
            let mutable lastRequest = None

            match lastRequest with
            | Some r -> window.cancelAnimationFrame r
            | _ -> ()

            lastRequest <- Some (window.requestAnimationFrame (fun _ ->
                Fable.Import.ReactDom.render(
                    view,
                    document.getElementById(placeholderId)
                )))

        let observer (notification : Notification<ReactElement>) =
            async {
                match notification with
                | OnNext view -> render view
                | OnError err -> printfn "renderReact, error: %A" err
                | _ -> ()
            }
        observer
