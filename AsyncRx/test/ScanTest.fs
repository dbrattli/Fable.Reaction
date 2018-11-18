module Tests.Scan

open Reaction

open Expecto
open Tests.Utils

exception MyError of string

[<Tests>]
let tests = testList "Query Tests" [

    testAsync "Test scanAsync" {
        // Arrange
        let scanner acc x =
            async {
                return acc + x
            }

        let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.scanInitAsync 0 scanner
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 15 "Should be equal"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 3; OnNext 6; OnNext 10; OnNext 15; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test scan" {
        // Arrange
        let scanner acc x =
            acc + x

        let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.scanInit 0 scanner
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 15 "Should be equal"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 3; OnNext 6; OnNext 10; OnNext 15; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test scan accumulator fails" {
        // Arrange
        let error = MyError "error"
        let scanner acc x =
            raise error
            0

        let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.scanInit 0 scanner
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv

        try
            do! obv.AwaitIgnore ()
        with
        | ex -> ()

        // Assert
        Expect.equal obv.Notifications.Count 1 "Should be equal"
    }
]