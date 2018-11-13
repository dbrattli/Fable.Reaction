# Reaction/AsyncRx

> Reaction.AsyncRx is a lightweight Async Reactive (AsyncRx) library for F#.

Reaction.AsyncRx is a library for asynchronous reactive programming, and is the implementation of Async Observables ([ReactiveX](http://reactivex.io/)) for F#. Reaction makes it easy to compose and combine streams of asynchronous event based data such as timers, mouse-moves, keyboard input, web requests and enables you to do operations such as:

- Filtering
- Transforming
- Aggregating
- Combining
- Time-shifting

Reaction.AsyncRx was designed specifically for targeting [Fable](http://fable.io/) which means that the code may be [transpiled](https://en.wikipedia.org/wiki/Source-to-source_compiler) to JavaScript, and thus the same F# code may be used both client and server side for full stack software development.

## Documentation

Please check out the [documentation](https://dbrattli.github.io/Reaction/)

## Install

```cmd
paket add Reaction.AsyncRx --project <project>
```
