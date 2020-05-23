module Tests.Observer

open FSharp.Control
open FSharp.Control.Core

open Expecto
open Tests.Utils
open FSharp.Control.AsyncRx
open System.Threading

exception MyError of string

[<Tests>]
let tests = testList "Observer Tests" [

    testAsync "Test safe observer empty sequence" {
        // Arrange
        let xs = fromNotification Seq.empty
        let obv = TestObserver<int> ()
        let safeObv = safeObserver obv AsyncDisposable.Empty

        // Act
        let! dispose = xs.SubscribeAsync safeObv

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test safe observer error sequence" {
        // Arrange
        let error = MyError "error"
        let xs = fromNotification [ OnError error ]
        let obv = TestObserver<int>()
        let safeObv = safeObserver obv AsyncDisposable.Empty

        // Act
        let! dispose = xs.SubscribeAsync safeObv
        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnError error ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test safe observer happy" {
        // Arrange
        let xs = AsyncRx.ofSeq [ 1..3]
        let obv = TestObserver<int>()
        let safeObv = safeObserver obv AsyncDisposable.Empty

        // Act
        let! dispose = xs.SubscribeAsync safeObv
        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test safe observer stops after completed" {
        // Arrange
        let xs = fromNotification [ OnNext 1; OnCompleted; OnNext 2]
        let obv = TestObserver<int>()
        let safeObv = safeObserver obv AsyncDisposable.Empty

        // Act
        let! dispose = xs.SubscribeAsync safeObv
        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnCompleted ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test safe observer stops after completed completed" {
        // Arrange
        let xs = fromNotification [ OnNext 1; OnCompleted; OnCompleted]
        let obv = TestObserver<int>()
        let safeObv = safeObserver obv AsyncDisposable.Empty

        // Act
        let! dispose = xs.SubscribeAsync safeObv
        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnCompleted ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test safe observer stops after error" {
        // Arrange
        let error = MyError "error"
        let xs = fromNotification [ OnNext 1; OnError error; OnNext 2]
        let obv = TestObserver<int>()
        let safeObv = safeObserver obv AsyncDisposable.Empty

        // Act
        let! dispose = xs.SubscribeAsync safeObv
        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnError error ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test safe observer stops after error error" {
        // Arrange
        let error = MyError "error"
        let xs = fromNotification [ OnNext 1; OnError error; OnError error]
        let obv = TestObserver<int>()
        let safeObv = safeObserver obv AsyncDisposable.Empty

        // Act
        let! dispose = xs.SubscribeAsync safeObv
        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnError error ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test auto-detach observer is disposing" {
        // Arrange
        let obv = TestObserver<int>()
        let mutable disposed = false

        let subscribeAsync (aobv : IAsyncObserver<int>) : Async<IAsyncRxDisposable> = async {
            let worker = async {
                for x in [1..5] do
                    do! aobv.OnNextAsync x
            }
            Async.Start' worker
            let cancel () = async {
                disposed <- true
            }
            return AsyncDisposable.Create cancel
        }
        let source = { new IAsyncObservable<int> with member __.SubscribeAsync o = subscribeAsync o }
        let xs = source |> AsyncRx.take 4

        // Act
        let! dispose = xs.SubscribeAsync obv
        do! obv.AwaitIgnore ()

        // Give dispose logic a run on the loop.
        do! Async.Sleep 10

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnCompleted ]

        Expect.isTrue disposed "Should be disposed"
        Expect.equal actual expected "Should be equal"
    }
  ]

