module Tests.Stream

open FSharp.Control
open Elmish.Streams

open Expecto

[<Tests>]
let tests = testList "Stream Tests" [

    testAsync "Test stream choose named should be found" {
        // Arrange
        let xs = AsyncRx.empty ()
        let stream = Stream [
            xs, "xs"
        ]

        // Act
        let result = Stream.chooseNamed "xs" Some stream

        // Assert
        match result with
        | Stream xss ->
            let _, name = xss.Head
            Expect.equal name "xs" "Should be equal"
    }

    testAsync "Test stream choose named should be found 2" {
        // Arrange
        let xs = AsyncRx.empty ()
        let stream = Stream [
            xs, "ys"
            xs, "xs"
            xs, "zs"
        ]

        // Act
        let result = Stream.chooseNamed "xs" Some stream

        // Assert
        match result with
        | Stream xss ->
            let _, name = xss.Head
            Expect.equal name "xs" "Should be equal"
    }

    testAsync "Test stream choose unknown named should not be found" {
        // Arrange
        let xs = AsyncRx.empty ()
        let stream = Stream [
            xs, "xs"
        ]

        // Act
        let result = Stream.chooseNamed "ys" Some stream

        // Assert
        match result with
        | Stream xss ->
            Expect.isEmpty xss "Should be empty"
    }

    testAsync "Test stream choose unknown named 2 should not be found" {
        // Arrange
        let stream = Stream.none


        // Act
        let result = Stream.chooseNamed "ys" Some stream

        // Assert
        match result with
        | Stream xss ->
            Expect.isEmpty xss "Should be empty"

    }
]