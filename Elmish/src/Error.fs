namespace Elmish.Streams

open System

module Error =
    let onError(text: string, ex: exn) = Console.Error.WriteLine("{0}: {1}", text, ex)