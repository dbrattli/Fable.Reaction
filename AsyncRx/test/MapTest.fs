module Tests.Map

open Reaction
open Tests.Utils

open Expecto

exception MyError of string

[<Tests>]
let tests = testList "Map Tests" [

    testAsync "Test map async" {
        // Arrange
        let mapper x =
            async {
                return x * 10
            }

        let xs = AsyncRx.single 42 |> AsyncRx.mapAsync mapper
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv
        let! latest= obv.Await ()

        // Assert
        Expect.equal latest 420 "Should be equal"
        Expect.equal obv.Notifications.Count 2 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 420; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test map sync" {
        // Arrange
        let mapper x =
            x * 10

        let xs = AsyncRx.single 42 |> AsyncRx.map mapper
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv
        let! latest= obv.Await ()

        // Assert
        Expect.equal latest 420 "Should be equal"
        Expect.equal obv.Notifications.Count 2 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 420; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test map mapper throws exception" {
        // Arrange
        let error = MyError "error"
        let mapper x =
            async {
                raise error
            }

        let xs = AsyncRx.single "error" |> AsyncRx.mapAsync mapper
        let obv = TestObserver<unit>()

        // Act
        let! cnl = xs.SubscribeAsync obv

        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        Expect.equal obv.Notifications.Count 1 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<unit> list = [ OnError error ]
        Expect.equal actual expected "Should be equal"
    }
]