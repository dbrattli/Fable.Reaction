module Tests.Create

open System.Threading.Tasks

open Reaction

open NUnit.Framework
open FsUnit
open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test single happy``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.single 42
    let obv = TestObserver<int>()

    // Act
    let! dispose = xs.SubscribeAsync obv

    // Assert
    let! latest = obv.Await ()
    latest |> should equal 42

    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 42; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test just dispose after subscribe``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.single 42
    let obv = TestObserver<int>()

    // Act
    let! subscription = xs.SubscribeAsync obv
    Async.StartImmediate (subscription.DisposeAsync ())

    // Assert
    //let actual = obv.Notifications |> Seq.toList
    //Assert.That(actual, Is.EquivalentTo([]))
    ()
}

[<Test>]
let ``Test ofSeq empty``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.ofSeq Seq.empty
    let obv = TestObserver<int>()

    // Act
    let! dispose = xs.SubscribeAsync obv

    do! obv.AwaitIgnore ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnCompleted ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test ofSeq non empty``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.ofSeq <| seq { 1 .. 5 }
    let obv = TestObserver<int>()

    // Act
    let! dispose = xs.SubscribeAsync obv
    do! obv.AwaitIgnore ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnCompleted ]

    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test ofSeq dispose after subscribe``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.ofSeq <| seq { 1 .. 5 }
    let obv = TestObserver<int>()

    // Act
    let! subscription = xs.SubscribeAsync obv
    do! subscription.DisposeAsync ()

    // Assert
    //let actual = obv.Notifications |> Seq.toList
    //Assert.That(actual, Is.EquivalentTo([]))
}

