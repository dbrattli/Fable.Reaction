module Tests.Main

open Expecto

[<Tests>]
let allTests =
    testList "all-tests" [
    ]

[<EntryPoint>]
let main argv =
    printfn "Running tests!"
    runTestsWithArgs defaultConfig argv allTests