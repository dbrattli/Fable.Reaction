module Tests.Concat

open System.Threading.Tasks

open Reaction

open NUnit.Framework
open FsUnit
open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test concat emtpy empty``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.empty ()
    let ys = AsyncRx.empty ()
    let zs = AsyncRx.concat [ xs; ys ]
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    try
        do! obv.AwaitIgnore ()
    with
    | _ -> ()

    // Assert
    obv.Notifications |> should haveCount 1
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test concat non emtpy empty``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.ofSeq <| seq { 1..3 }
    let ys = AsyncRx.empty ()
    let zs = AsyncRx.concat [ xs; ys ]
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 3
    obv.Notifications |> should haveCount 4
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test concat empty non empty``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.empty ()
    let ys = AsyncRx.ofSeq <| seq { 1..3 }
    let zs = AsyncRx.concat [ xs; ys ]
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 3
    obv.Notifications |> should haveCount 4
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test concat two``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.ofSeq <| seq { 1..3 }
    let ys = AsyncRx.ofSeq <| seq { 4..6 }
    let zs = AsyncRx.concat [ xs; ys ]
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 6
    obv.Notifications |> should haveCount 7
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

(*
[<Test>]
let ``Test concat +``() = toTask <| async {
    // Arrange
    let xs = ofSeq <| seq { 1..3 }
    let ys = ofSeq <| seq { 4..6 }
    let zs = xs + ys
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 6
    obv.Notifications |> should haveCount 7
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}
*)

[<Test>]
let ``Test concat three``() = toTask <| async {
    // Arrange
    let a = AsyncRx.ofSeq <| seq { 1..2 }
    let b = AsyncRx.ofSeq <| seq { 3..4 }
    let c = AsyncRx.ofSeq <| seq { 5..6 }
    let xs = AsyncRx.concat [ a; b; c ]
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 6
    obv.Notifications |> should haveCount 7
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

exception MyError of string

[<Test>]
let ``Test concat fail with non emtpy ``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let xs = AsyncRx.fail error
    let ys = AsyncRx.ofSeq <| seq { 1..3 }
    let zs = AsyncRx.concat [ xs; ys ]
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
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