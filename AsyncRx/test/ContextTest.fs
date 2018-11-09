module Tests.Context

open System
open System.Threading
open System.Threading.Tasks

open NUnit.Framework
open FsUnit

open Reaction
open Test.Reaction.Context

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test that scheduler ran`` () = toTask <| async {
    // Arrange
    let mutable ran = false

    let ctx = TestSynchronizationContext ()

    let task = async {
        ran <- true
    }
    // Act
    do! ctx.RunAsync task

    // Assert
    Assert.IsTrue ran
}

[<Test>]
let ``Test that scheduler ran with delay`` () = toTask <| async {
    // Arrange
    let ctx = TestSynchronizationContext ()
    let mutable ran = false

    let task = async {
        do! ReactionContext.SleepAsync 100
        ran <- true
    }
    // Act
    do! ctx.RunAsync task

    // Assert
    Assert.IsTrue ran
}

[<Test>]
let ``Test that scheduler delay takes no or little real time`` () = toTask <| async {
    // Arrange
    let ctx = TestSynchronizationContext ()
    let stopWatch = System.Diagnostics.Stopwatch ()

    let task = async {
        stopWatch.Start ()
        do! ReactionContext.SleepAsync 1000
        stopWatch.Stop ()
    }
    // Act
    do! ctx.RunAsync task

    // Assert
    printfn "Elapsed %A" stopWatch.ElapsedMilliseconds
    Assert.IsTrue (stopWatch.ElapsedMilliseconds < 50L)
}

[<Test>]
let ``Test that scheduler delay takes virtual time`` () = toTask <| async {
    // Arrange
    let ctx = TestSynchronizationContext ()
    let mutable start = DateTime.Now
    let mutable stop = DateTime.Now

    let task = async {
        start <- ReactionContext.Now
        do! ReactionContext.SleepAsync 1000
        stop <- ReactionContext.Now
    }
    // Act
    do! ctx.RunAsync task

    // Assert
    let diff = (stop - start).TotalMilliseconds
    Assert.IsTrue ((diff < 1010.0) && (diff > 990.0))
}
