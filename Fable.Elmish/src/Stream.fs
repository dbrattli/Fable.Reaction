namespace Reaction

open Reaction

/// Stream extension methods
[<RequireQualifiedAccess>]
type Stream =
    /// Map stream from one message type to another.
    static member map (mapper: 'msgIn -> 'msgOut) (stream: Stream<'msgIn, 'name>) :Stream<'msgOut, 'name> =
        match stream with
        | Stream (msgs', name) ->
            Stream.Stream (msgs' |> AsyncRx.map mapper, name)
        | Streams q ->
            Streams [
                for query in q do
                    yield Stream.map mapper query
            ]
        | Dispose -> Dispose

    /// Applies the given chooser function to each element of the stream and
    /// returns the stream comprised of the results for each element where the
    /// function returns Some with some value.
    static member choose (chooser: 'msgIn -> 'msgOut option) (stream: Stream<'msgIn, 'name>) : Stream<'msgOut, 'name> =
        match stream with
        | Stream (msgs', name) ->
            Stream.Stream (msgs' |> AsyncRx.choose chooser, name)
        | Streams q ->
            Streams [
                for query in q do
                    yield Stream.choose chooser query
            ]
        | Dispose -> Dispose

    /// Selects the stream wit the given name and applies the given chooser
    /// function to each element of the stream and returns the stream comprised
    /// of the results for each element where the function returns Some with
    /// some value.
    static member chooseNamed (name: 'name) (chooser: 'msgIn -> 'msgOut option) (stream: Stream<'msgIn, 'name>) : Stream<'msgOut, 'name> =
        match stream with
        | Stream (xs, name') when name'=name ->
            Stream.Stream (xs |> AsyncRx.choose chooser, name)
        | Streams xss ->
            match xss with
            | xs :: tail ->
                match xs with
                | Stream (xs, name') when name'=name ->
                    Stream.Stream (xs |> AsyncRx.choose chooser, name)
                | Streams xss ->
                    let xs = Stream.chooseNamed name chooser (Stream.Streams xss)
                    match xs with
                    | Dispose -> Stream.chooseNamed name chooser (Stream.Streams tail)
                    | _ -> xs
                | _ -> Stream.chooseNamed name chooser (Stream.Streams tail)
            | [] -> Dispose
        | _ -> Dispose

/// Named message stream. Can be a single Stream, a collection of Streams or Dispose to remove a
/// stream.
and Stream<'msg, 'name> =
    /// Named message stream
    | Stream of IAsyncObservable<'msg>*'name
    /// Collection of named streams
    | Streams of Stream<'msg, 'name> list
    /// Dispose message stream. This is the Stream equivalent of None
    | Dispose
        interface IAsyncObservable<'msg> with
            member this.SubscribeAsync obv =
                let rec flatten streams =
                    [
                        for stream in streams do
                            match stream with
                            | Stream (xs, _) ->
                                yield xs
                            | Streams xss ->
                                yield! flatten xss
                            | _ -> ()
                    ]
                async {
                    match this with
                    | Stream (xs, _) ->
                        return! xs.SubscribeAsync obv
                    | Streams xss ->
                        let xs = flatten xss |> AsyncRx.mergeSeq
                        return! xs.SubscribeAsync obv
                    | _ ->
                        return AsyncDisposable.Empty
                }


