namespace FSharp.Control

open System.Threading

module Core =
    let infinite = Seq.initInfinite id

    type Async with
        /// Starts the asynchronous computation in the thread pool, or
        /// immediately for Fable. Do not await its result. If no cancellation
        /// token is provided then the default cancellation token is used.
        static member Start' (computation:Async<unit>, ?cancellationToken: CancellationToken) : unit =
            #if FABLE_COMPILER
                Async.StartImmediate (computation, ?cancellationToken=cancellationToken)
            #else
                Async.Start (computation, ?cancellationToken=cancellationToken)
            #endif

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]

    module Async =
        let empty = async { () }

        let noop = fun _ -> empty
