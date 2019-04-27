module Tests.Debounce

open System.Threading.Tasks

open Reaction
open Test.Reaction.Context

open NUnit.Framework
open FsUnit
open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test debounce single value``() = toTask <| async {
    // Arrange

    let dispatch, obs = AsyncRx.stream ()
    let xs = obs |> AsyncRx.debounce 100
    let obv = TestObserver<int> ()
    let ctx = TestSynchronizationContext ()
    let mutable latest = -1

    // Act
    let task = async {
        let! sub = xs.SubscribeAsync obv
        printfn "A"
        do! dispatch.OnNextAsync 42
        printfn "B"
        do! Async.Sleep 250
        printfn "C"
        do! dispatch.OnCompletedAsync ()
        printfn "D"


        printfn "Await ----------------------"
        let! latest' = obv.Await ()
        latest <- latest'
        printfn "Exiting task ---------------"
        ()
    }
    do! ctx.RunAsync task

    // Assert
    latest |> should equal 42
    obv.Notifications |> should haveCount 2
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 42; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test debounce two immediate values``() = toTask <| async {
    // Arrange

    let dispatch, obs = AsyncRx.stream ()
    let xs = obs |> AsyncRx.debounce 100
    let obv = TestObserver<int>()
    let ctx = TestSynchronizationContext ()
    let mutable latest = -1

    // Act
    let task = async {
        let! sub = xs.SubscribeAsync obv
        do! dispatch.OnNextAsync 42
        do! dispatch.OnNextAsync 43
        do! Async.Sleep 150
        do! dispatch.OnCompletedAsync ()

        let! latest' = obv.Await ()
        latest <- latest'
        ()
    }
    do! ctx.RunAsync task

    // Assert
    latest |> should equal 43
    obv.Notifications |> should haveCount 2
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 43; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test debounce two separate values``() = toTask <| async {
    // Arrange
    let dispatch, obs = AsyncRx.stream ()
    let xs = obs |> AsyncRx.debounce 100
    let obv = TestObserver<int>()
    let ctx = TestSynchronizationContext ()
    let mutable latest = -1

    let task = async {
        do! dispatch.OnNextAsync 42
        do! Async.Sleep 150
        do! dispatch.OnNextAsync 43
        do! Async.Sleep 150
        do! dispatch.OnCompletedAsync ()

        let! latest' = obv.Await ()
        latest <- latest'
        ()
    }

    // Act
    let! sub = xs.SubscribeAsync obv
    do! ctx.RunAsync task

    // Assert
    latest |> should equal 43
    obv.Notifications |> should haveCount 3
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 42; OnNext 43; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}