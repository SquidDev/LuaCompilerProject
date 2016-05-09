module LuaCP.Test.Runner

open NUnitLite

[<EntryPoint>]
let main argv = AutoRun().Execute argv
