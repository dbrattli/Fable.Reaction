module Tests.Observer

open System.Threading.Tasks

open Reaction
open Reaction.Core

open NUnit.Framework
open FsUnit
open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test safe observer empty sequence``() = toTask <| async {
    // Arrange
    let xs = fromNotification Seq.empty
    let obv = TestObserver<int>()
    let safeObv = safeObserver obv

    // Act
    let! dispose = xs.SubscribeAsync safeObv

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

exception MyError of string

[<Test>]
let ``Test safe observer error sequence``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let xs = fromNotification [ OnError error ]
    let obv = TestObserver<int>()
    let safeObv = safeObserver obv

    // Act
    let! dispose = xs.SubscribeAsync safeObv
    try
        do! obv.AwaitIgnore ()
    with
    | _ -> ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnError error ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test safe observer happy``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.ofSeq [ 1..3]
    let obv = TestObserver<int>()
    let safeObv = safeObserver obv

    // Act
    let! dispose = xs.SubscribeAsync safeObv
    do! obv.AwaitIgnore ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test safe observer stops after completed``() = toTask <| async {
    // Arrange
    let xs = fromNotification [ OnNext 1; OnCompleted; OnNext 2]
    let obv = TestObserver<int>()
    let safeObv = safeObserver obv

    // Act
    let! dispose = xs.SubscribeAsync safeObv
    do! obv.AwaitIgnore ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnCompleted ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test safe observer stops after completed completed``() = toTask <| async {
    // Arrange
    let xs = fromNotification [ OnNext 1; OnCompleted; OnCompleted]
    let obv = TestObserver<int>()
    let safeObv = safeObserver obv

    // Act
    let! dispose = xs.SubscribeAsync safeObv
    do! obv.AwaitIgnore ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnCompleted ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test safe observer stops after error``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let xs = fromNotification [ OnNext 1; OnError error; OnNext 2]
    let obv = TestObserver<int>()
    let safeObv = safeObserver obv

    // Act
    let! dispose = xs.SubscribeAsync safeObv
    try
        do! obv.AwaitIgnore ()
    with
    | _ -> ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnError error ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test safe observer stops after error error``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let xs = fromNotification [ OnNext 1; OnError error; OnError error]
    let obv = TestObserver<int>()
    let safeObv = safeObserver obv

    // Act
    let! dispose = xs.SubscribeAsync safeObv
    try
        do! obv.AwaitIgnore ()
    with
    | _ -> ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnError error ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

