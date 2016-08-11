module TypesGalore.Main

// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
[<EntryPoint>]
let main argv = 
    let scp = new Scope()
    let func = scp.Add(1)
    let a = func.Add(LStr "foo" |> ELiteral)
    let b = func.Add(LStr "bar" |> ELiteral)
    func.AddE(EIf(RArg 0, a, b))

    printfn "%s" (scp.ToString())
    0 // return an integer exit code
