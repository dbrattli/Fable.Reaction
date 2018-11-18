module Tests.Merge

open System.Threading.Tasks

open Reaction

open Expecto
open Tests.Utils

exception  MyError of string

[<Tests>]
let tests = testList "Merge Tests" [

    testAsync "Test merge non empty emtpy" {
        // Arrange
        let xs = seq { 1..5 } |> AsyncRx.ofSeq
        let ys = AsyncRx.empty<int> ()
        let zs = AsyncRx.ofSeq [ xs; ys ] |> AsyncRx.mergeInner
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        let! latest= obv.Await ()

        // Assert
        Expect.equal latest 5 "Should be equal"
        Expect.equal obv.Notifications.Count 6 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test merge empty non emtpy" {
        // Arrange
        let xs = AsyncRx.empty<int> ()
        let ys = seq { 1..5 } |> AsyncRx.ofSeq
        let zs = AsyncRx.ofSeq [ xs; ys ] |> AsyncRx.mergeInner
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        let! latest= obv.Await ()

        // Assert
        Expect.equal latest 5 "Should be equal"
        Expect.equal obv.Notifications.Count 6 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test merge error error" {
        // Arrange
        let error = MyError "error"
        let xs = AsyncRx.fail error
        let ys = AsyncRx.fail error
        let zs = AsyncRx.ofSeq [ xs; ys ] |> AsyncRx.mergeInner
        let obv = TestObserver<int> ()

        // Act
        let! sub = zs.SubscribeAsync obv

        try
            do! obv.Await () |> Async.Ignore
        with
        | _ -> ()

        // Assert
        Expect.equal obv.Notifications.Count 1 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnError error ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test merge two" {
        // Arrange
        let xs  = seq { 1..3 } |> AsyncRx.ofSeq
        let ys = seq { 4..5 } |> AsyncRx.ofSeq
        let zs = AsyncRx.ofSeq [ xs; ys ] |> AsyncRx.mergeInner
        let obv = TestObserver<int> ()

        // Act
        let! sub = zs.SubscribeAsync obv
        do! obv.AwaitIgnore ()

        // Assert
        //obv.Notifications |> should haveCount 6
        let actual = obv.Notifications |> Seq.toList
        Expect.contains actual (OnNext 1) "Should contain the element"
        Expect.contains actual (OnNext 2) "Should contain the element"
        Expect.contains actual (OnNext 3) "Should contain the element"
        Expect.contains actual (OnNext 4) "Should contain the element"
        Expect.contains actual (OnNext 5) "Should contain the element"
        Expect.contains actual (OnCompleted) "Should contain the element"
    }
]