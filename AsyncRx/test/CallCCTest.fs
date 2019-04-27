module Tests.CallCC

open System.Threading.Tasks

open Reaction.AsyncRx

open NUnit.Framework
open FsUnit
open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test callCC``() = toTask <| async {
    // Arrange
    let f x = single (x * 3)
    let g x = single (x - 2)

    let h x abort : AsyncObservable<int> =
        printfn "x=%A" x
        if x = 5 then
            f x
        else
            abort (OnNext -1)

    let xs = asyncRx {
        let! x = single 5

        yield! callCC (fun abort ->
            async {
                return reaction {
                    let! a = async { return 42 }
                    let! y = h x abort
                    //yield! g y
                    yield! async { return 42 }
                }
            })

    }
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv.PostAsync
    //do! obv.AwaitIgnore ()
    do! Async.Sleep 1000

    // Assert
    //obv.Notifications |> should haveCount 2
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}
