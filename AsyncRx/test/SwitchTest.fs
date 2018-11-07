module Tests.Switch

open System.Threading.Tasks
open Reaction.AsyncRx
open Reaction.AsyncRx.Streams

open NUnit.Framework
open FsUnit
open Tests.Utils

exception MyError of string

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test switch2``() = toTask <| async {
    // Arrange
    let obvA, a = stream<int> ()
    let obvB, b = stream<int> ()
    let obvX, xs = stream<IAsyncObservable<int>> ()
    let ys = xs |> AsyncRx.flatMapLatest (fun x -> x)

    let obv = TestObserver<int>()

    // Act
    let! sub = ys.SubscribeAsync obv

    do! obvX.OnNextAsync a
    do! Async.Sleep 100
    do! obvA.OnNextAsync 1
    do! obvA.OnNextAsync 2

    do! Async.Sleep 200
    do! obvX.OnNextAsync b
    do! Async.Sleep 100

    do! obvB.OnNextAsync 10
    do! obvB.OnNextAsync 20
    do! obvB.OnNextAsync 30

    do! obvA.OnNextAsync 3

    do! Async.Sleep 100

    do! obvA.OnCompletedAsync ()
    do! obvB.OnCompletedAsync ()
    do! obvX.OnCompletedAsync ()

    do! Async.Sleep 1000
    try
        do! obv.AwaitIgnore ()
        ()
    with
    | _ -> ()

    // Assert
    obv.Notifications |> should haveCount 6
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 2; OnNext 10; OnNext 20; OnNext 30; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}
