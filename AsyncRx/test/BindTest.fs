module Tests.Bind

open System.Threading.Tasks

open Reaction.AsyncRx

open NUnit.Framework
open FsUnit
open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test bind empty``() = toTask <| async {
    // Arrange
    let xs = AsyncRx.empty ()
    let zs = xs |> AsyncRx.flatMap (fun x -> x)
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
let ``Test bind some``() = toTask <| async {
    // Arrange
    let xs = fromNotification [ OnNext 1; OnNext 2; OnNext 3; OnCompleted]
    let zs = xs |> AsyncRx.flatMap (fun x -> AsyncRx.single x)
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    do! obv.AwaitIgnore ()

    // Assert
    obv.Notifications |> should haveCount 4
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test bind monad law left identity``() = toTask <| async {
    // return x >>= f is the same thing as f x

    // Arrange
    let f x = AsyncRx.single (x * 10)
    let xs = AsyncRx.single 42 |> AsyncRx.flatMap f
    let ys = f 42
    let obv1 = TestObserver<int>()
    let obv2 = TestObserver<int>()

    // Act
    do! xs.RunAsync obv1
    let! x = obv1.Await ()

    do! ys.RunAsync obv2
    let! y = obv2.Await ()

    // Assert
    x |> should equal y
    x |> should equal 420
}

[<Test>]
let ``Test bind monad law right identity``() = toTask <| async {
    // m >>= return is no different than just m

    // Arrange
    let m = AsyncRx.single 42
    let xs = m |> AsyncRx.flatMap AsyncRx.single
    let obv1 = TestObserver<int>()
    let obv2 = TestObserver<int>()

    // Act
    do! m.RunAsync obv1
    let! x = obv1.Await ()

    do! xs.RunAsync obv2
    let! y = obv2.Await ()

    // Assert
    x |> should equal y
    x |> should equal 42
}

[<Test>]
let ``Test bind monad law associativity``() = toTask <| async {
    // (m >>= f) >>= g is just like doing m >>= (\x -> f x >>= g)

    // Arrange
    let m = AsyncRx.single 42
    let f x = AsyncRx.single (x * 1000)
    let g x = AsyncRx.single (x * 42)

    let xs = m |> AsyncRx.flatMap f |> AsyncRx.flatMap g
    let ys = m |> AsyncRx.flatMap (fun x -> f x |> AsyncRx.flatMap g)

    let obv1 = TestObserver<int>()
    let obv2 = TestObserver<int>()

    // Act
    do! xs.RunAsync obv1
    let! x = obv1.Await ()

    do! ys.RunAsync obv2
    let! y = obv2.Await ()

    // Assert
    x |> should equal y
    x |> should equal 1764000
}

[<Test>]
let ``Test bind expression some``() = toTask <| async {
    // Arrange
    let xs = fromNotification [ OnNext 1; OnNext 2; OnNext 3; OnCompleted]
    let ys = asyncRx {
        let! x = xs
        yield x * 2
    }
    let obv = TestObserver<int>()

    // Act
    let! sub = ys.SubscribeAsync obv
    do! obv.AwaitIgnore ()

    // Assert
    obv.Notifications |> should haveCount 4
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 2; OnNext 4; OnNext 6; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test bind expression some for``() = toTask <| async {
    // Arrange
    let xs = fromNotification [ OnNext 1; OnNext 2; OnNext 3; OnCompleted]
    let ys = asyncRx {
        for x in xs do
            yield x * 2
    }
    let obv = TestObserver<int>()

    // Act
    let! sub = ys.SubscribeAsync obv
    do! obv.AwaitIgnore ()

    // Assert
    obv.Notifications |> should haveCount 4
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 2; OnNext 4; OnNext 6; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test bind expression some return bang``() = toTask <| async {
    // Arrange
    let xs = fromNotification [ OnNext 1; OnNext 2; OnNext 3; OnCompleted]
    let ys = asyncRx {
        let! x = xs
        yield! AsyncRx.single (x * 2)
    }
    let obv = TestObserver<int>()

    // Act
    let! sub = ys.SubscribeAsync obv
    do! obv.AwaitIgnore ()

    // Assert
    obv.Notifications |> should haveCount 4
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 2; OnNext 4; OnNext 6; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}
