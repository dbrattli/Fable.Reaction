# Howto use with Elmish

To use Fable Reaction with Elmish you need to call the `Program.withQuery` with your query. The query function takes an `IAsyncObservable<'msg>` and returns a possibibly transformed `IAsyncObservable<'msg>`.

```f#
open Reaction        // 1. Open Reaction, for operators such as delay.
open Fable.Reaction  // 2. Open Fable.Reaction

// (your Elmish program here)

let query msgs = // 3. Add reactive query
    msgs |> delay 1000

Program.mkSimple init update view
|> Program.withQuery query       // 4. Enable the query in Elmish
|> Program.withReact "elmish-app"
|> Program.run
```

## Loading initial State

To load initial state from the server without using commands (`Cmd`) you create an Async Observable using `ofPromise` can concat (`+`) the result into the message stream. Thus the message stream in the example below will start with the initialCountLoaded message.

```f#
let loadCount =
    ofPromise (fetchAs<int> "/api/init" [])
        |> map (Ok >> InitialCountLoaded)
        |> catch (Error >> InitialCountLoaded >> single)

let query msgs =
    loadCount + msgs
```

## Doing side effects per message

In the example below we flat map (map and merge) the result of querying Wikipedia back into the message stream.
The `flatMapLatest` operator is a combination of the `map` and `switchLatest` operators. This operator works like
`flatMap` but will auto-cancel any ongoing fetch operation if a new query is made before the previous result is ready.

```f#
let query msgs =
    msgs
    |> choose Msg.asKeyboardEvent
    |> map targetValue
    |> filter (fun term -> term.Length > 2 || term.Length = 0)
    |> debounce 750          // Pause for 750ms
    |> distinctUntilChanged  // Only if the value has changed
    |> flatMapLatest searchWikipedia

```

