module LuaCP.Parser.Core

open NUnit.Framework
open LuaCP.Parser
open LuaCP.Parser.CharParsers
open LuaCP.Parser.Recogniser

[<Test>]
let ``Null rules``() = 
    let G = new Grammar<_>()
    let A = G.MakeRule "A"
    let B = G.MakeRule "B"
    G.AddPattern A [||]
    G.AddPattern A [| Rule B |]
    G.AddPattern B [| Rule A |]
    G.Bake()
    match parse A "" with
    | Failure items -> Assert.Fail(sprintf "Expected success, got %A" items)
    | Success items -> CollectionAssert.AreEquivalent(items, [ A; B ], "For empty match")

[<Test>]
let ``Basic calculator``() = 
    let G = new Grammar<_>()
    let sum = G.MakeRule "Sum"
    let product = G.MakeRule "Product"
    let factor = G.MakeRule "Factor"
    let number = G.MakeRule "Number"
    G.AddPattern sum [| Rule sum
                        anyOf "+-"
                        Rule product |]
    G.AddPattern sum [| Rule product |]
    G.AddPattern product [| Rule product
                            anyOf "*/"
                            Rule factor |]
    G.AddPattern product [| Rule factor |]
    G.AddPattern factor [| char '('
                           Rule sum
                           char ')' |]
    G.AddPattern factor [| Rule number |]
    G.AddPattern number [| range '0' '9' |]
    G.AddPattern number [| range '0' '9'
                           Rule number |]
    G.Bake()
    match parse sum "2*3+(4*5)" with
    | Failure items -> Assert.Fail(sprintf "Expected success, got %A" items)
    | Success items -> CollectionAssert.AreEquivalent(items, [ sum ])
