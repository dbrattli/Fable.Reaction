module Tests.GroupBy

open System.Threading.Tasks
open Reaction

open NUnit.Framework
open FsUnit
open Tests.Utils

exception  MyError of string

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test groupby empty``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.empty<int> ()
            |> AsyncRx.groupBy (fun _ -> 42)
            |> AsyncRx.flatMap (fun x -> x)
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
    let expected : Notification<int> list = [ OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test groupby error``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let xs = AsyncRx.fail<int> error
            |> AsyncRx.groupBy (fun _ -> 42)
            |> AsyncRx.flatMap (fun x -> x)
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

[<Test>]
let ``Test groupby 2 groups``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.ofSeq [1; 2; 3; 4; 5; 6]
            |> AsyncRx.groupBy (fun x -> x % 2)
            |> AsyncRx.flatMap (fun x -> x)
    let obv = TestObserver<int> ()

    // Act
    let! sub = xs.SubscribeAsync obv

    try
        do! obv.AwaitIgnore ()
    with
    | _ -> ()

    // Assert
    obv.Notifications |> should haveCount 7
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test groupby cancel``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.ofSeq [1; 2; 3; 4; 5; 6]
            |> AsyncRx.groupBy (fun x -> x % 2)
            |> AsyncRx.flatMap (fun x -> x)
    let obv = TestObserver<int> ()

    // Act
    let! sub = xs.SubscribeAsync obv
    do! sub.DisposeAsync ()

    // Assert
    obv.Notifications.Count |> should be (lessThan 8)
}