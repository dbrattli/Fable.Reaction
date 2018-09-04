namespace Fable.Reaction

open Elmish
open Reaction

[<RequireQualifiedAccess>]
//[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Program =
    let dispatcher<'msg> (obv : AsyncObserver<'msg>) =
        MailboxProcessor.Start(fun inbox ->
            let rec messageLoop _ = async {
                let! msg = inbox.Receive ()
                do! obv.OnNextAsync msg
                return! messageLoop ()
            }
            messageLoop ()
        )

    /// Attach a reaction query to the message (Msg) stream of an Elmish program.
    let withQuery (query: AsyncObservable<'msg> -> AsyncObservable<'msg>) (program: Elmish.Program<_,_,_,_>) =
        let obv, stream = stream<'msg> ()
        let mutable dispatch' : Dispatch<'msg> = ignore

        let view model dispatch =
            dispatch' <- dispatch
            let disp = ((dispatcher obv).Post)
            program.view model disp

        let msgObserver n = async {
            match n with
            | OnNext x -> dispatch' x
            | OnError ex -> program.onError ("Reaction error", ex)
            | OnCompleted -> ()
        }

        let main = async {
            let msgs = query stream
            do! msgs.RunAsync msgObserver
        }
        main |> Async.StartImmediate

        { program with view = view }
