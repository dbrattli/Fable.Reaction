# Fable.Elmish.Reaction Operators

Fable.Elmish.Reaction may use any operator from [AsyncRx](https://dbrattli.github.io/Reaction/asyncrx/Operators.html).
In addition the following Fable.Elmish.Reaction specific operators have been defined.

## AsyncRx.ofPromise

The operator `AsyncRx.ofPromise` converts a Promise to an Async Observable. If the promise resolves with a value,
then the observable will produce a single value (`OnNext`) and then complete with `OnCompleted`.

This operator is very handy for merging (`flatMap`) web request responses into the reactive pipeline.

## AsyncRx.ofMouseMove

The operator `AsyncRx.ofMouseMove` produces events of type `Fable.Import.Browser.MouseEvent`  by attaching
an event listener on the `Window`. Thus you want to use this to get mouse moves independent of
any HTML elements. To mouse moves from a particular HTML element you should probably use the `OnMouseMove`
property and dispatch a message instead.
