=============
Query Builder
=============

Queries may be written by composing functions or using query expressions.

.. code:: fsharp

    let xs = asyncRx {
        yield 42
    }

This expression is equivalent to:

.. code:: fsharp

    let xs = AsyncRx.single 42

You can also yield multiple values:

.. code:: fsharp

    let xs = asyncRx {
        yield 42
        yield 43
    }

This is equivalent to:

.. code:: fsharp

    let xs = AsyncRx.ofSeq [42; 43]

    // or

    let xs = AsyncRx.concat [AsyncRx.single 42; AsyncRx.single 43]

Flat mapping
============

.. code:: fsharp

    let xs = asyncRx {
        let! i = AsyncRx.single 42
        yield i*10
    }

This is equivalent to:

.. code:: fsharp

    let xs =
        AsyncRx.single 42
        |> AsyncRx.flatMap (fun i ->
            AsyncRx.single (i * 10))
    }

More advanced example
=====================

These two examples below are equivalent:

.. code:: fsharp

    Seq.toList "TIME FLIES LIKE AN ARROW"
    |> Seq.mapi (fun i c -> i, c)
    |> AsyncRx.ofSeq
    |> AsyncRx.flatMap (fun (i, c) ->
        AsyncRx.fromMouseMoves ()
        |> AsyncRx.delay (100 * i)
        |> AsyncRx.map (fun m -> Letter (i, string c, int m.clientX, int m.clientY)))

The above query may be written in query expression style:

.. code:: fsharp

    asyncRx {
        let! i, c = Seq.toList "TIME FLIES LIKE AN ARROW"
                    |> Seq.mapi (fun i c -> i, c)
                    |> AsyncRx.ofSeq
        let ms = AsyncRx.fromMouseMoves () |> delay (100 * i)
        for m in ms do
            yield Letter (i, string c, int m.clientX, int m.clientY)
    }
