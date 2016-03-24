open System
open System.Reflection
open NUnitLite
open NUnit.Common

[<EntryPoint>]
let main argv = (new AutoRun()).Execute(Assembly.GetEntryAssembly(), Console.Out, Console.In, argv)
