namespace LuaCP

open System
open NUnit.Framework

type Data() = 
    // This works as a member function, but not a let binding.
    static member Make([<ParamArray>] args : Object []) = TestCaseData(args).SetName(sprintf "%A" args)

    static member Named (name : string, [<ParamArray>] args : Object []) = 
        TestCaseData(args).SetName(sprintf "%s: %A" name args)
