# Fable Reaction

```fs
Fable |> AsyncRx
```

<img src="/AsyncRx/logo/logo.png" width="100">

Fable.Reaction is a collection of projects that combines the power of asynchronous reactive (AsyncRx) functional programming (RFP) with F#, [Fable](http://fable.io/) and [Elmish](https://elmish.github.io/) applications.

- **FSharp.Control.AsyncRx** - implementation of Async Observables in F# for .NET and Fable.
- **Fable.Reaction** - for the use of Reaction with Fable
- **Reaction.AspNetCore.Middleware** - using Reaction with WebSockets in ASP.NET Core.

Use reactive programming for easier handling of events such as keyboard, mouse, network and websockets. Fable.Reaction gives you the power to:

- Transform - change messages
- Filter - reducing the message stream
- Time-shift - delay messages
- Partition - split a stream into multiple streams
- Combine - merge multiple streams into one

Fable.Reaction is is built on the Async Reactive ([Rx](http://reactivex.io/)) library

## Documentation

Please check out the [documentation](http://fablereaction.rtfd.io/)

Fable.Reaction combines the power of reactive programming with [Fable](http://fable.io/) and [Elmish](https://elmish.github.io/) applications.

## Install

```cmd
paket add Fable.Reaction --project <project>
```

