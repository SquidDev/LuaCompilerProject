namespace LuaCP

open System
open NUnit.Framework

type Data() = 
    // This works as a member function, but not a let binding.
    static member Make([<ParamArray>] args : Object []) = TestCaseData(args).SetName(sprintf "%A" args)