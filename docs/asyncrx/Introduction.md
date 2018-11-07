# Reaction

Reaction is a library for asynchronous and reactive programming, and is an implementation of Async Observables ([ReactiveX](http://reactivex.io/)) for F# and [Fable](http://fable.io/). Reaction makes it easy to compose and combine streams of asynchronous event based data such as timers, mouse-moves, keyboard input, web requests and enables you to do operations such as:

- Filter
- Transform
- Aggregate
- Combine
- Time-shift

Reaction was designed spesifically for targeting [Fable](http://fable.io/) which means that the code may be [transpiled](https://en.wikipedia.org/wiki/Source-to-source_compiler) to JavaScript, and thus the same F# code may be used both client and server side for full stack software development.

See [Fable Reaction](https://github.com/dbrattli/Fable.Reaction) for use of Reaction with Elmish and the model view update (MVU) style architecture.