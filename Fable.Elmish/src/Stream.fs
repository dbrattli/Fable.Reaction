namespace Elmish.Streams

open FSharp.Control

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
    /// None - no stream. Use to dispose a previously subscribed stream.
    let none : Stream<'msg, 'name> =
        Stream []

    /// Map stream from one message type to another.
    let map (f: 'a -> 'msg) : Stream<'a, 'name> -> Stream<'msg, 'name> = function
        | Stream xss ->
            xss
            |> List.map (fun (xs, name) -> xs |> AsyncRx.map f, name)
            |> Stream

    /// Filter stream based on given predicate.
    let filter (predicate: 'msg -> bool) : Stream<'msg, 'name> -> Stream<'msg, 'name> = function
        | Stream xss ->
            xss
            |> List.map (fun (xs, name) -> xs |> AsyncRx.filter predicate, name)
            |> Stream

    /// Aggregate multiple streams
    let batch (streams: #seq<Stream<'msg, 'name>>) : Stream<'msg, 'name> =
        Stream [
            for (Stream xss) in streams do
                yield! xss
        ]

    /// Tap into stream and print messages to console. The tag is a
    /// string used to give yourself a hint of where the tap is
    /// inserted. Returns the stream unmodified.
    let tap tag : Stream<'msg, 'name> -> Stream<'msg, 'name> = function
        | Stream xss ->
            xss
            |> List.map (fun (xs, name) -> xs |> AsyncRx.tapOnNext (printfn "[Elmish.Streams] \"%s\" (%A) - %A" tag   name), name)
            |> Stream

    /// Applies the given chooser function to each element of the stream and
    /// returns the stream comprised of the results for each element where the
    /// function returns with Some value.
    let choose (chooser: 'a -> 'msg option) : Stream<'a, 'name> -> Stream<'msg, 'name> =
        function
        | Stream xss ->
            xss
            |> List.map (fun (xs, name) -> xs |> AsyncRx.choose chooser, name)
            |> Stream

    /// Applies the given chooser function to each element of the stream and
    /// returns the stream comprised of the results for each element where the
    /// function returns with None value.
    let chooseNot chooser =
        filter (chooser >> Option.isNone)

    /// Selects the stream with the given name and applies the given chooser
    /// function to each element of the stream and returns the stream comprised
    /// of the results for each element where the function returns with
    /// Some value.
    let chooseNamed (name: 'name) (chooser: 'a -> 'msg option) : Stream<'a, 'name> -> Stream<'msg, 'name> =
        function
        | Stream xss ->
            xss
            |> List.filter (fun (xs, name') -> name = name')
            |> List.map (fun (xs, name') -> xs |> AsyncRx.choose chooser, name')
            |> Stream

    /// **Description**
    ///
    /// Composes a sub-stream of a sub-component into the message stream of the main component. Returns the
    /// main message stream where the sub-messages have been sent thought the sub-component and the result
    /// is then merged back into the main message stream.
    ///
    /// **Parameters**
    ///   * `stream` - parameter of type `'model -> Stream<'subMsg,'name> -> Stream<'subMsg,'name>`
    ///   * `model` - parameter of type `'subModel`
    ///   * `toSubMsg` - parameter of type `'msg -> 'subMsg option`
    ///   * `toMsg` - parameter of type `'subMsg -> 'msg`
    ///   * `name` - parameter of type `'name`
    ///   * `msgs` - parameter of type `Stream<'msg,'name>`
    ///
    /// **Output Type**
    ///   * `Stream<'msg,'name>`
    ///
    let subStream (stream: 'subModel -> Stream<'subMsg, 'name> -> Stream<'subMsg, 'name>) (subModel: 'subModel) (toSubMsg: 'msg -> 'subMsg option) (toMsg: 'subMsg -> 'msg) (name : 'name) (msgs: Stream<'msg, 'name>) =
        let subMsgs = Stream [ msgs |> AsyncRx.choose toSubMsg, name]
        let subMsgs' = stream subModel subMsgs |> map toMsg
        let msgs' = msgs |> chooseNot toSubMsg

        batch [
            subMsgs'
            msgs'
        ]
