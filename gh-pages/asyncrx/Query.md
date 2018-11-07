# Query Builder

Queries may be written by composing functions or using query expressions.

```fs
let xs = reaction {
    yield 42
}
```

This expression is equivalent to:

```fs
let xs = AsyncObservable.single 42
```

You can also yield multiple values:

```fs
let xs = reaction {
    yield 42
    yield 43
}
```

This is equivalent to:

```fs
let xs = AsyncObservable.ofSeq [42; 43]

// or

let xs = AsyncObservable.concat [AsyncObservable.single 42; AsyncObservable.single 43]
```

## Flat mapping

```fs
let xs = reaction {
    let! i = single 42
    yield i*10
}
```

This is equivalent to:

```fs
let xs =
    AsyncObservable.single 42
    |> flatMap (fun i ->
        AsynbObservable.single (i * 10))
}
```

## More advanced example

These two examples below are equivalent:

```fs
Seq.toList "TIME FLIES LIKE AN ARROW"
|> Seq.mapi (fun i c -> i, c)
|> ofSeq
|> flatMap (fun (i, c) ->
    fromMouseMoves ()
    |> delay (100 * i)
    |> map (fun m -> Letter (i, string c, int m.clientX, int m.clientY)))
```

The above query may be written in query expression style:

```fs
reaction {
    let! i, c = Seq.toList "TIME FLIES LIKE AN ARROW"
                |> Seq.mapi (fun i c -> i, c)
                |> ofSeq
    let ms = fromMouseMoves () |> delay (100 * i)
    for m in ms do
        yield Letter (i, string c, int m.clientX, int m.clientY)
}
```