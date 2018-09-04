namespace Fable.Reaction

open Elmish
open Reaction

[<RequireQualifiedAccess>]
module Program =
    /// Attach a Reaction query to the message (Msg) stream of an Elmish program.
    let withQuery (query: AsyncObservable<'msg> -> AsyncObservable<'msg>) (program: Elmish.Program<_,_,_,_>) =
        let mutable dispatch' : Dispatch<'msg> = ignore
        let mb, stream = mbStream<'msg> ()

        let view model dispatch =
            dispatch' <- dispatch
            program.view model mb.Post

        let main = async {
            let msgObserver n = async {
                match n with
                | OnNext x -> dispatch' x
                | OnError ex -> program.onError ("Query error", ex)
                | OnCompleted -> ()
            }

            let msgs = query stream
            do! msgs.RunAsync msgObserver
        }
        main |> Async.StartImmediate

        { program with view = view }

