module Tests.Create

open System.Threading.Tasks

open FSharp.Control
open Expecto

open Tests.Utils

[<Tests>]
let tests = testList "Create Tests" [

    testAsync "Test single happy" {
        // Arrange
        let xs = AsyncRx.single 42
        let obv = TestObserver<int> ()

        // Act
        let! dispose = xs.SubscribeAsync obv

        // Assert
        let! latest = obv.Await ()
        Expect.equal latest 42 "Should be equal"

        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 42; OnCompleted ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test just dispose after subscribe" {
        // Arrange
        let xs = AsyncRx.single 42
        let obv = TestObserver<int> ()

        // Act
        let! subscription = xs.SubscribeAsync obv
        Async.StartImmediate (subscription.DisposeAsync ())

        // Assert
        //let actual = obv.Notifications |> Seq.toList
        //Assert.That(actual, Is.EquivalentTo([]))
        ()
    }

    testAsync "Test ofSeq empty"  {
        // Arrange
        let xs = AsyncRx.ofSeq Seq.empty
        let obv = TestObserver<int> ()

        // Act
        let! dispose = xs.SubscribeAsync obv

        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnCompleted ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test ofSeq non empty" {
        // Arrange
        let xs = seq { 1 .. 5 } |> AsyncRx.ofSeq
        let obv = TestObserver<int> ()

        // Act
        let! dispose = xs.SubscribeAsync obv
        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnCompleted ]

        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test ofSeq dispose after subscribe" {
        // Arrange
        let xs = AsyncRx.ofSeq <| seq { 1 .. 5 }
        let obv = TestObserver<int> ()

        // Act
        let! subscription = xs.SubscribeAsync obv
        do! subscription.DisposeAsync ()

        // Assert
        //let actual = obv.Notifications |> Seq.toList
        //Assert.That(actual, Is.EquivalentTo([]))
    }
]
