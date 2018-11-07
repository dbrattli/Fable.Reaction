# Fable.Reaction Operators

Fable.Reaction may use any operator from [Reaction](https://dbrattli.github.io/Reaction/pages/Operators.html).
In addition the following Fable.Reaction specific operators have been defined.

## ofPromise

The operator `ofPromise` converts a Promise to an Async Observable. If the promise resolves with a value,
then the observable will produce a single value (`OnNext`) and then complete with `OnCompleted`.

This operator is very handy for merging (`flatMap`) web request responses into the reactive pipeline.

## ofMouseMove

The operator `ofMouseMove` produces events of type `Fable.Import.Browser.MouseEvent`  by attaching
an event listener on the `Window`. Thus you want to use this to get mouse moves independent of
any HTML elements. To mouse moves from a particular HTML element you should probably use the `OnMouseMove`
property and dispatch a message instead.
