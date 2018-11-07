module Tests.TakeUntil

open System.Threading.Tasks
open Reaction.AsyncRx
open Reaction.AsyncRx.Streams

open NUnit.Framework
open FsUnit
open Tests.Utils

exception  MyError of string

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test take until empty``() = toTask <| async {
    // Arrange
    let obvX, xs = stream<int> ()
    let obvY, ys = stream<bool> ()
    let zs = xs |> AsyncRx.takeUntil ys

    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    do! Async.Sleep 100
    do! obvX.OnNextAsync 1
    do! obvX.OnNextAsync 2
    do! obvY.OnNextAsync true
    do! Async.Sleep 500
    do! obvX.OnNextAsync 3
    do! obvX.OnCompletedAsync ()

    try
        do! obv.AwaitIgnore ()
    with
    | _ -> ()

    // Assert
    obv.Notifications |> should haveCount 3
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}
