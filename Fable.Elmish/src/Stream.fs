namespace Reaction

/// Subscription -- A named Async Observable to be subscribed.
type Subscription<'msg, 'name> = IAsyncObservable<'msg>*'name

/// Stream - container for subscriptions that may produce messages
type Stream<'msg, 'name> = Stream of Subscription<'msg, 'name> list with
    interface IAsyncObservable<'msg> with
        member this.SubscribeAsync obv =
            async {
                let (Stream stream) = this
                match stream with
                | [] ->
                    return AsyncDisposable.Empty
                | [xs, _] ->
                    return! xs.SubscribeAsync obv
                | xss ->
                    let obs =
                        xss
                        |> List.map (fun (xs, _) -> xs)
                        |> AsyncRx.mergeSeq
                    return! obs.SubscribeAsync obv
            }

/// Stream extension methods
[<RequireQualifiedAccess>]
module Stream =
    /// None - no stream. Use to dispose a previously subscribed stream`
    let none : Stream<'msg, 'name> =
        Stream []

    /// Map stream from one message type to another.
    let map (f: 'a -> 'msg) : Stream<'a, 'name> -> Stream<'msg, 'name> =
        function
        | Stream xss ->
            xss
            |> List.map (fun (xs, name) -> (xs |> AsyncRx.map f, name))
            |> Stream

    /// Aggregate multiple streams
    let batch (streams: #seq<Stream<'msg, 'name>>) : Stream<'msg, 'name> =
        Stream [
            for (Stream xss) in streams do
                yield! xss
        ]

    /// Applies the given chooser function to each element of the stream and
    /// returns the stream comprised of the results for each element where the
    /// function returns Some with some value.
    let choose (chooser: 'a -> 'msg option) : Stream<'a, 'name> -> Stream<'msg, 'name> =
        function
        | Stream xss ->
            xss
            |> List.map (fun (xs, name) -> xs |> AsyncRx.choose chooser, name)
            |> Stream

    /// Selects the stream wit the given name and applies the given chooser
    /// function to each element of the stream and returns the stream comprised
    /// of the results for each element where the function returns Some with
    /// some value.
    let chooseNamed (name: 'name) (chooser: 'a -> 'msg option) : Stream<'a, 'name> -> Stream<'msg, 'name> =
        function
        | Stream xss ->
            xss
            |> List.filter (fun (xs, name') -> name = name')
            |> List.map (fun (xs, name') -> xs |> AsyncRx.choose chooser, name')
            |> Stream


