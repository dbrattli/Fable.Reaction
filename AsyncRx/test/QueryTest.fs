module Tests.Query

open System.Threading.Tasks

open FSharp.Control
open Expecto

open Tests.Utils

[<Tests>]
let tests = testList "Query Tests" [

    testAsync "test empty query" {
        // Arrange
        let xs = asyncRx {
            ()
        }
        let obv = TestObserver<unit>()

        // Act
        let! dispose = xs.SubscribeAsync obv

        // Assert
        try
            let! latest = obv.Await ()
            ()
        with
            | :? TaskCanceledException -> ()

        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<unit> list = [ OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "test query let!" {
        // Arrange
        let obv = TestObserver<int>()

        let xs = asyncRx {
            let! a = seq [1; 2] |> AsyncRx.ofSeq
            let! b = seq [3; 4] |> AsyncRx.ofSeq

            yield a + b
        }

        // Act
        let! subscription = xs.SubscribeAsync obv
        let! latest = obv.Await ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 4; OnNext 5; OnNext 5; OnNext 6; OnCompleted ]
        Expect.containsAll actual expected "Should contain all"
    }

    testAsync "test query yield!" {
        // Arrange
        let obv = TestObserver<int>()

        let xs = asyncRx {
            yield! AsyncRx.single 42
        }

        // Act
        let! subscription = xs.SubscribeAsync obv
        let! latest = obv.Await ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 42;OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "test query yield" {
        // Arrange
        let obv = TestObserver<int>()

        let xs = asyncRx {
            yield 42
        }

        // Act
        let! subscription = xs.SubscribeAsync obv
        let! latest = obv.Await ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 42; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "test query combine" {
        // Arrange
        let obv = TestObserver<int>()

        let xs = asyncRx {
            yield 42
            yield 43
        }

        // Act
        let! subscription = xs.SubscribeAsync obv
        let! latest = obv.Await ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 42; OnNext 43; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "test query for in observable" {
        // Arrange
        let obv = TestObserver<int>()

        let xs = asyncRx {
            let xs = AsyncRx.ofSeq [1; 2; 3]
            for x in xs do
                yield x * 10
        }

        // Act
        let! subscription = xs.SubscribeAsync obv
        let! latest = obv.Await ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 10; OnNext 20; OnNext 30; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "test query for in seq" {
        // Arrange
        let obv = TestObserver<int>()

        let xs = asyncRx {
            for x in [1; 2; 3] do
                yield x * 10
        }

        // Act
        let! subscription = xs.SubscribeAsync obv
        let! latest = obv.Await ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 10; OnNext 20; OnNext 30; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "test query async" {
        // Arrange
        let obv = TestObserver<int>()

        let xs = asyncRx {
            let! b = async { return 42 }
            yield b + 2
        }

        // Act
        let! subscription = xs.SubscribeAsync obv
        let! latest = obv.Await ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 44; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }
]