module Tests.Filter

open FSharp.Control

open Expecto
open Tests.Utils

exception MyError of string

[<Tests>]
let tests = testList "Filter Tests" [

    testAsync "Test filter async" {
        // Arrange
        let predicate x =
            async {
                return x < 3
            }

        let xs = seq { 1..5 } |> AsyncRx.ofSeq |> AsyncRx.filterAsync predicate
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 2 "should be equal"
        Expect.equal obv.Notifications.Count 3 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }


    testAsync "Test filter" {
        // Arrange
        let predicate x = x < 3

        let xs = seq { 1..5 } |> AsyncRx.ofSeq |> AsyncRx.filter predicate
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 2 "Should be equal"
        Expect.equal obv.Notifications.Count 3 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test filter predicate throws exception" {
        // Arrange
        let error = MyError "error"
        let predicate x =
            async {
                raise error
                return true
            }

        let xs = seq { 1..5 } |> AsyncRx.ofSeq |> AsyncRx.filterAsync predicate
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv

        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        Expect.equal obv.Notifications.Count 1 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnError error ]
        Expect.equal actual expected "Should be equal"
    }

]
