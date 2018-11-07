# Getting Started

Below is the simple "Hello, world!" example of a single value stream being subscribed using an observer function.

```fs
open Reaction

let main = async {
    let xs = AsyncObservable.single "Hello, world!"

    let obv n =
        async {
            match n with
            | OnNext x -> printfn "OnNext: %d" x
            | OnError ex -> printfn "OnError: %s" (ex.ToString ())
            | OnCompleted -> printfn "OnCompleted"
        }

    let! disposable = xs.SubscribeAsync obv
    ()
}

Async.Start main
```