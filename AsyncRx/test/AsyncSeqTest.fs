module Tests.ToAsyncSeq

open System.Collections.Generic
open System.Threading.Tasks

open Reaction.AsyncRx

open NUnit.Framework
open FsUnit
open FSharp.Control

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test to async seq``() = toTask <| async {
    let xs = AsyncRx.ofSeq <| seq { 1..5 } |> AsyncRx.toAsyncSeq
    let result = List<int> ()

    let each x = async {
        result.Add x
    }

    // Act
    do! xs |> AsyncSeq.iterAsync each

    // Assert
    result.Count |> should equal 5
    let expected = seq { 1..5 } |> Seq.toList
    Assert.That(result, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test seq to async seq to async observerable to async seq``() = toTask <| async {
    let xs = seq { 1..5 } |> AsyncSeq.ofSeq |> AsyncRx.ofAsyncSeq |> AsyncRx.toAsyncSeq
    let result = List<int> ()

    let each x = async {
        result.Add x
    }

    // Act
    do! xs |> AsyncSeq.iterAsync each

    // Assert
    result.Count |> should equal 5
    let expected = seq { 1..5 } |> Seq.toList
    Assert.That(result, Is.EquivalentTo(expected))
}