module LuaCP.Parser.Parsers

open FParsec
open LuaCP.Parser.Extensions
open LuaCP.Tree
open LuaCP.Reporting

type Parser<'t> = Parser<'t, unit>

let betweenL (popen : Parser<_, _>) (pclose : Parser<_, _>) (p : Parser<_, _>) 
    label = 
    let expectedLabel = expected label
    let notClosedError (pos : FParsec.Position) = 
        messageError 
            (sprintf "The %s opened at %s was not closed." label 
                 (pos.ToString()))
    fun (stream : CharStream<_>) -> 
        // The following code might look a bit complicated, but that's mainly
        // because we manually apply three parsers in sequence and have to merge
        // the errors when they refer to the same parser state.
        let state0 = stream.State
        let reply1 = popen stream
        if reply1.Status = Ok then 
            let stateTag1 = stream.StateTag
            let reply2 = p stream
            
            let error2 = 
                if stateTag1 <> stream.StateTag then reply2.Error
                else mergeErrors reply1.Error reply2.Error
            if reply2.Status = Ok then 
                let stateTag2 = stream.StateTag
                let reply3 = pclose stream
                
                let error3 = 
                    if stateTag2 <> stream.StateTag then reply3.Error
                    else mergeErrors error2 reply3.Error
                if reply3.Status = Ok then Reply(Ok, reply2.Result, error3)
                else 
                    Reply
                        (reply3.Status, 
                         mergeErrors error3 
                             (notClosedError (state0.GetPosition(stream))))
            else Reply(reply2.Status, reply2.Error)
        else 
            let error = 
                if state0.Tag <> stream.StateTag then reply1.Error
                else expectedLabel
            Reply(reply1.Status, error)

let (<||>) (p1 : Parser<'a, 'u>) (p2 : Parser<'a, 'u>) : Parser<'a, 'u> = 
    fun stream -> 
        let mutable state = CharStreamState stream
        let reply1 = p1 stream
        if reply1.Status = Ok then reply1
        else 
            let rep1State = stream.StateTag
            stream.BacktrackTo(state)
            let reply2 = p2 stream
            if reply2.Status = Ok || stream.StateTag > rep1State then reply2
            else reply1

let (<!>) (p : Parser<_, _>) label : Parser<_, _> = 
    fun stream -> 
        printfn "%A: Entering %s" stream.Position label
        let reply = p stream
        printfn "%A: Leaving %s (%A)" stream.Position label reply.Status
        if reply.Status = Ok then printfn "%A: %A" stream.Position reply.Result
        else printfn "%A: %A" stream.Position reply.Error
        reply

let (<?!>) (p : Parser<_, _>) label : Parser<_, _> = p <?> label <!> label
let (<??!>) (p : Parser<_, _>) label : Parser<_, _> = p <??> label <!> label

let chain (initial : Parser<'t, 'u>) (remaining : 't -> Parser<'t, 'u>) 
    (input : CharStream<'u>) = 
    let rec parse (input : CharStream<'u>) (result : Reply<'t>) oldState = 
        if result.Status = Ok && input.StateTag <> oldState then 
            let state = input.StateTag
            parse input (remaining result.Result input) state
        else result
    
    let state = input.StateTag
    parse input (initial input) state

let refL (x : string) (p : Parser<_>) = new NamedReference<_, unit>(x, p)

let unboundRefL<'t> (x : string) = 
    let p = new NamedReference<'t, unit>(x)
    p.AsParser, p

let boundRefL<'t> (x : string) (c : Parser<'t>) = 
    let p = new NamedReference<'t, unit>(x, c)
    p.AsParser, p

let longestChoiceL<'t> (p : seq<Parser<'t>>) (x : string) = 
    let p = new LongestMatch<'t, unit>(x, p)
    p.AsParser, p

let bOpt p = opt p |>> (fun x -> x.IsSome)

let blacklist (illegal : Set<string>) (p : Parser<string, _>) 
    (stream : CharStream<_>) = 
    let current = stream.State
    let result = p stream
    if result.Status = Ok && illegal.Contains result.Result then 
        stream.BacktrackTo current
        Reply(Error, unexpectedString ("identifier" + result.Result))
    else result
let whitelist (valid : Set<string>) (p : Parser<string, _>) 
    (stream : CharStream<_>) = 
    let current = stream.State
    let result = p stream
    if result.Status = Ok && not (valid.Contains result.Result) then 
        stream.BacktrackTo current
        Reply(Error, unexpectedString ("identifier" + result.Result))
    else result

let inline getPosition (stream : CharStream<_>) = 
    new Position(int32 (stream.Line), int32 (stream.Column), 
                 int32 (stream.Index))

let withPosition (p : Parser<#INode, _>) (stream : CharStream<_>) = 
    let start = getPosition stream
    let result = p stream
    if result.Status = Ok then 
        result.Result.Position <- new Range(stream.Name, start, 
                                            getPosition stream)
    result