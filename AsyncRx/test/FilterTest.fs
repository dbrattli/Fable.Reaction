module Tests.Filter

open System.Threading.Tasks

open Reaction

open NUnit.Framework
open FsUnit
open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test filter async``() = toTask <| async {
    // Arrange
    let predicate x =
        async {
            return x < 3
        }

    let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.filterAsync predicate
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 2
    obv.Notifications |> should haveCount 3
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}


[<Test>]
let ``Test filter``() = toTask <| async {
    // Arrange
    let predicate x = x < 3

    let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.filter predicate
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 2
    obv.Notifications |> should haveCount 3
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

exception MyError of string

[<Test>]
let ``Test filter predicate throws exception``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let predicate x =
        async {
            raise error
            return true
        }

    let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.filterAsync predicate
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv

    try
        do! obv.AwaitIgnore ()
    with
    | _ -> ()

    // Assert
    obv.Notifications |> should haveCount 1
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnError error ]
    Assert.That(actual, Is.EquivalentTo(expected))
}


