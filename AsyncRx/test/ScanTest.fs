module Tests.Scan

open System.Threading.Tasks

open Reaction

open NUnit.Framework
open FsUnit
open Tests.Utils

exception MyError of string

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test scanAsync``() = toTask <| async {
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
    result |> should equal 15
    obv.Notifications |> should haveCount 6
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 3; OnNext 6; OnNext 10; OnNext 15; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test scan``() = toTask <| async {
    // Arrange
    let scanner acc x =
        acc + x

    let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.scanInit 0 scanner
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 15
    obv.Notifications |> should haveCount 6
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 3; OnNext 6; OnNext 10; OnNext 15; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test scan accumulator fails``() = toTask <| async {
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
    obv.Notifications |> should haveCount 1
}