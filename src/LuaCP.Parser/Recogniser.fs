module LuaCP.Parser.Recogniser

open System.Collections.Generic

type ParseResult<'t> = 
    | Failure of Element<'t> list
    | Success of RuleGroup<'t> list

type private StateCell<'t> = 
    { Rule : Pattern<'t>
      Start : int
      Current : int }

type private State<'t> = HashSet<StateCell<'t>>

let private dumpCell cell = 
    let tString i x = 
        let prefix = 
            if i = cell.Current then "• "
            else ""
        prefix + (x.ToString())
    
    let mapped = Seq.mapi tString cell.Rule.Contents |> String.concat " "
    
    let suffix = 
        if cell.Current = cell.Rule.Contents.Length then " •"
        else ""
    sprintf "%s -> %s%s (%d)" cell.Rule.Group.Name mapped suffix cell.Start

let private dumpState i currentState = 
    printfn "State %d" i
    for cell in currentState do
        printfn "%s" (dumpCell cell)
    printfn ""

let private matches target state = 
    let contents = state.Rule.Contents
    let offset = state.Current
    if offset < contents.Length then 
        match contents.[offset] with
        | Rule child when child = target -> true
        | _ -> false
    else false

let private doIteration (position : int) (states : List<State<_>>) (queue : Queue<StateCell<_>>) (value : _) 
    (hasValue : bool) = 
    let currentState = states.[position]
    for item in currentState do
        queue.Enqueue item
    while queue.Count > 0 do
        let cell = queue.Dequeue()
        let offset = cell.Current
        let pattern = cell.Rule
        if offset < pattern.Contents.Length then 
            match pattern.Contents.[offset] with
            | Terminal term when hasValue && term.Matches value -> 
                states.[position + 1].Add { Rule = pattern
                                            Start = cell.Start
                                            Current = offset + 1 }
                |> ignore
            | Terminal _ -> () // Fails
            | Rule rule -> 
                // Predict step
                for child in rule.Patterns do
                    let newCell = 
                        { Rule = child
                          Start = position
                          Current = 0 }
                    if currentState.Add newCell then queue.Enqueue newCell
                if rule.IsNullable then 
                    let nextCell = 
                        { Rule = pattern
                          Start = cell.Start
                          Current = offset + 1 }
                    if currentState.Add nextCell then queue.Enqueue nextCell
        else 
            // Complete step
            for parent in states.[cell.Start] |> Seq.filter (matches pattern.Group) do
                let newCell = 
                    { Rule = parent.Rule
                      Start = parent.Start
                      Current = parent.Current + 1 }
                if currentState.Add newCell then queue.Enqueue newCell

let parse<'t> (rule : RuleGroup<'t>) string = 
    let states = new List<State<'t>>()
    let queue = new Queue<StateCell<_>>()
    states.Add(new State<'t>())
    for pattern in rule.Patterns do
        states.[0].Add { Rule = pattern
                         Start = 0
                         Current = 0 }
        |> ignore
    let mutable position = 0
    for chr in string do
        if states.[position].Count > 0 then 
            states.Add(new State<'t>()) |> ignore
            doIteration position states queue chr true
            position <- position + 1
    doIteration position states queue Unchecked.defaultof<_> false
    let last = states.[position]
    let items = 
        Seq.filter (fun (state : StateCell<'t>) -> state.Start = 0 && state.Current = state.Rule.Contents.Length) last 
        |> Seq.toList
    if last.Count = 0 || items.IsEmpty then 
        let position = 
            if last.Count = 0 then position - 1
            else position
        
        let previous = states.[position]
        
        let expected = 
            previous
            |> Seq.choose (fun x -> 
                   let contents = x.Rule.Contents
                   let offset = x.Current
                   if offset < contents.Length then Some contents.[offset]
                   else None)
            |> Seq.distinct
            |> Seq.toList
        Failure expected
    else 
        items
        |> List.map (fun x -> x.Rule.Group)
        |> List.distinct
        |> Success
