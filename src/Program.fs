namespace Fable.Reaction

open Reaction

/// Dispatch - feed new message into the processing loop
type Dispatch<'msg> = 'msg -> unit

/// Program type captures various aspects of program behavior
type Program<'arg, 'model, 'msg, 'view> = {
    init : 'arg -> 'model
    update : 'model -> 'msg -> 'model
    view : Dispatch<'msg> -> 'model -> 'view
    stream : AsyncObserver<'msg>*AsyncObservable<'msg>
    observer : Notification<'view> -> Async<unit>
    onError : (string*exn) -> unit
}

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Program =
    let dispatcher (obv : AsyncObserver<'msg>) =
        MailboxProcessor.Start(fun inbox ->
            let rec messageLoop _ = async {
                let! msg = inbox.Receive ()
                do! obv.OnNextAsync msg
                return! messageLoop ()
            }
            messageLoop ()
        )
    let noop (x : Notification<_>) =
        async { () }

    /// Typical program, new commands are produced by `init` and `update` along with the new state.
    let mkProgram
        (init : 'arg -> 'model)
        (update : 'model -> 'msg -> 'model)
        (view : Dispatch<'msg> -> 'model -> 'view) =
        { init = init
          update = update
          view = view
          stream  = stream ()
          observer = noop
          onError = Log.onError }

    let withReaction (query: AsyncObservable<'msg> -> AsyncObservable<'msg>) program =
        let dispatch, msgs = program.stream
        { program with stream = dispatch, query msgs }

    let withReact (elem : string) program =
        { program with observer = renderReact elem }

    let run (program: Program<unit, 'model, 'msg, 'view>) =
        let main = async {
            let initialModel = program.init ()
            let dispatch = fst program.stream |> dispatcher
            let view (model : 'model) : 'view =
                program.view dispatch.Post model

            // Render inital view to avoid startWith below
            do! view initialModel |> OnNext |> program.observer

            let views =
                snd program.stream
                |> scan initialModel program.update
                |> map view

            do! views.RunAsync program.observer
        }

        main |> Async.StartImmediate