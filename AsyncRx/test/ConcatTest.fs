module Tests.Concat

open System.Threading.Tasks

open Reaction

open Expecto
open Tests.Utils

exception MyError of string

[<Tests>]
let tests = testList "Merge Tests" [

    testAsync "Test concat emtpy empty" {
        // Arrange
        let xs = AsyncRx.empty ()
        let ys = AsyncRx.empty ()
        let zs = AsyncRx.concatSeq [ xs; ys ]
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        Expect.equal obv.Notifications.Count 1 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test concat non emtpy empty" {
        // Arrange
        let xs = seq { 1..3 } |> AsyncRx.ofSeq
        let ys = AsyncRx.empty ()
        let zs = AsyncRx.concatSeq [ xs; ys ]
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 3 "Should be equal"
        Expect.equal obv.Notifications.Count 4 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test concat empty non empty" {
        // Arrange
        let xs = AsyncRx.empty ()
        let ys = seq { 1..3 } |> AsyncRx.ofSeq
        let zs = AsyncRx.concatSeq [ xs; ys ]
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 3 "Should be equal"
        Expect.equal obv.Notifications.Count 4 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test concat two" {
        // Arrange
        let xs = seq { 1..3 } |> AsyncRx.ofSeq
        let ys = seq { 4..6 } |> AsyncRx.ofSeq
        let zs = AsyncRx.concatSeq [ xs; ys ]
        let obv = TestObserver<int> ()

        // Act
        let! sub = zs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 6 "Should be equal"
        Expect.equal obv.Notifications.Count 7 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test concat ++" {
        // Arrange
        let xs = seq { 1..3 } |> AsyncRx.ofSeq
        let ys = seq { 4..6 } |> AsyncRx.ofSeq
        let zs = xs ++ ys
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 6 "Should be equal"
        Expect.equal obv.Notifications.Count 7 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test concat three" {
        // Arrange
        let a = seq { 1..2 } |> AsyncRx.ofSeq
        let b = seq { 3..4 } |> AsyncRx.ofSeq
        let c = seq { 5..6 } |> AsyncRx.ofSeq
        let xs = AsyncRx.concatSeq [ a; b; c ]
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv
        let! result = obv.Await ()

        // Assert
        Expect.equal result 6 "Should be equal"
        Expect.equal obv.Notifications.Count 7 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test concat fail with non emtpy" {
        // Arrange
        let error = MyError "error"
        let xs = AsyncRx.fail error
        let ys = seq { 1..3 } |> AsyncRx.ofSeq
        let zs = AsyncRx.concatSeq [ xs; ys ]
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
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