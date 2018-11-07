# Scaling Fable.Reaction Applications


From `Fable.Reaction` ~> 3 `withQuery` now takes a query that takes the current Model (`'model`) as the first argument like this:

```fs
let withQuery (query: 'model -> IAsyncObservable<'msg> -> IAsyncObservable<'msg>*'key) (program: Elmish.Program<_,_,_,_>) =
```

And there is a helper for calling sub-queries for pages and componets that will help you with (un)wrapping to and from (sub-)messages.

```fs
let withSubQuery subquery submodel msgs wrapMsg unwrapMsg : IAsyncObservable<_> * string =
```

Thus a sub-query can be called like this:

```fs
let query (model: Model) (msgs: IAsyncObservable<Msg>) =
    match model.PageModel with
    | HomePageModel ->
        msgs, "home"
    ...
    | TomatoModel m ->
        Program.withSubQuery Tomato.query m msgs TomatoMsg Msg.asTomatoMsg
```

The `Msg.asTomatoMsg` is a helper function you can declare as an extension on Msg (`'msg -> 'submsg option`). It takes a stream of messages and returns a stream of sub-messages e.g:

```fs
type Msg =
    ...
    | TomatoMsg of Tomato.Msg

    static member asTomatoMsg = function
        | TomatoMsg tmsg -> Some tmsg
        | _ -> None
```

Pages with multiple components (or Programs with concurrently active Pages) will need to compose the returned async observables from each sub-component together using e.g. `AsyncRx.merge` and join the keys to keep them unique, e.g. (+) for strings. We can make helper functions for this as well.
