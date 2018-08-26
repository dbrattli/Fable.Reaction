namespace Fable.Reaction

open Reaction

/// Dispatch - feed new message into the processing loop
type Dispatch<'msg> = 'msg -> unit

/// Program type captures various aspects of program behavior
type Program<'arg, 'model, 'msg, 'view> = {
    init : 'arg -> 'model
    update : 'model -> 'msg -> 'model
    view : 'model -> Dispatch<'msg> -> 'view
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
    let mkReaction
        (init : 'arg -> 'model)
        (update : 'model -> 'msg -> 'model)
        (view : 'model -> Dispatch<'msg> -> 'view) =
        { init = init
          update = update
          view = view
          stream  = stream ()
          observer = noop
          onError = Log.onError }

    let withRx (query: AsyncObservable<'msg> -> AsyncObservable<'msg>) program =
        let dispatch, msgs = program.stream
        { program with stream = dispatch, query msgs }

    let withReact (elem : string) program =
        { program with observer = renderReact elem }

    let run (program: Program<unit, 'model, 'msg, 'view>) =
        let main = async {
            let initialModel = program.init ()
            let dispatch = fst program.stream |> dispatcher
            let view (model : 'model) : 'view =
                program.view model dispatch.Post

            let elems =
                snd program.stream
                |> scan initialModel program.update
                |> map view

            do! elems.RunAsync program.observer
        }

        main |> Async.StartImmediate