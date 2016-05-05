module LuaCP.Types.StandardLibraries

open LuaCP.IR
open LuaCP.Types

let private tStr, tNum, tInt, tBoo = Primitives.String, Primitives.Number, Primitives.Integer, Primitives.Boolean
let private tVoid = TupleType.Empty
let private lStr x = Literal(Literal.String x)
let private func x = Function(Single(x, None), tVoid)
let private funcL x y = Function(Single(x, None), Single(y, None))
let private tabl x = Table(x |> Set.ofList, OperatorHelpers.Empty)

let Base = 
    tabl [ { Key = lStr "print"
             Value = Function(Single([], Some Dynamic), tVoid)
             ReadOnly = true }
           { Key = lStr "tostring"
             Value = funcL [ Dynamic ] [ tStr ]
             ReadOnly = true } ]