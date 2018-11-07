module Tests.Map

open System.Threading.Tasks

open Reaction.AsyncRx
open Reaction.AsyncRx.Streams

open NUnit.Framework
open FsUnit
open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test map async``() = toTask <| async {
    // Arrange
    let mapper x =
        async {
            return x * 10
        }

    let xs = AsyncRx.single 42 |> AsyncRx.mapAsync mapper
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv
    let! latest= obv.Await ()

    // Assert
    latest |> should equal 420
    obv.Notifications |> should haveCount 2
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 420; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test map sync``() = toTask <| async {
    // Arrange
    let mapper x =
        x * 10

    let xs = AsyncRx.single 42 |> AsyncRx.map mapper
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv
    let! latest= obv.Await ()

    // Assert
    latest |> should equal 420
    obv.Notifications |> should haveCount 2
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 420; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

exception MyError of string

[<Test>]
let ``Test map mapper throws exception``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let mapper x =
        async {
            raise error
        }

    let xs = AsyncRx.single "error" |> AsyncRx.mapAsync mapper
    let obv = TestObserver<unit>()

    // Act
    let! cnl = xs.SubscribeAsync obv

    try
        do! obv.AwaitIgnore ()
    with
    | _ -> ()

    // Assert
    obv.Notifications |> should haveCount 1
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<unit> list = [ OnError error ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

type Msg = int
let getMessageAsync () = async {
    return 42;
}
let myStream () =
    let subscribe (obs: IAsyncObserver<Msg>) : Async<IAsyncDisposable> =
        let mutable running = true

        async {
            let worker () = async {
                while running do
                    let! msg = getMessageAsync ()
                    do! obs.OnNextAsync msg
                }

            Async.Start (worker ())

            let cancel () = async {
                running <- false
            }

            return AsyncDisposable.Create(cancel)
        }

    AsyncRx.create(subscribe)

open Reaction
open System.Threading

let myStream' () =
    let worker (obv: IAsyncObserver<Msg>) (token: CancellationToken)  = async {
        while not token.IsCancellationRequested do
            let! msg = getMessageAsync ()
            do! obv.OnNextAsync msg

    }

    Create.ofAsyncWorker(worker)

let myStream2 () =
    let dispatch, obs = stream<Msg> ()

    let worker () = async {
        while true do
            let! msg = getMessageAsync ()
            do! dispatch.OnNextAsync msg
    }

    Async.Start (worker ())
    obs