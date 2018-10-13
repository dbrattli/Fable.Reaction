# Fable.Reaction

Fable Reaction combines the power of reactive programming with [Fable](http://fable.io/) and [Elmish](https://elmish.github.io/) applications.

Use reative programming on the Elmish message stream for easier handling of events such as keyboard, mouse, network and websockets. Fable.Reaction gives you the power to:

- Transform - change messages
- Filter - reducing the message stream
- Time-shift - delay messages
- Partition - split a stream into multiple streams
- Combine - merge multiple streams into one

Fable.Reaction is is built on the [Reaction](https://github.com/dbrattli/Reaction) Async Reactive ([Rx](http://reactivex.io/)) library

## When to use Fable.Reaction

The Elm(ish) way of structuring applications solves most scenarios, and for many applications it may not make sense to use Fable.Reaction. The scenarios where it however may make sense to use Fable.Reaction are:

- Your update function gets very complicated and handles messages paired as "do" and "done", e.g `DoFetchResults/DoneFetchResults`. Fable Reaction helps you keep the update function clean and simple.

- You need to combine and orchestrate *multiple* sources of events, e.g mouse moves, clicks, fetch results, websocket events.

- You need to time-shift events, e.g delay, throttle, debounce or set timeouts for actions.

- You need to group and aggregate messages by category, count and, or time.