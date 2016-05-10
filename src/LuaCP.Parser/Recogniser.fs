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

let private makeList (x : IEnumerable<'t>) = new List<'t>(x)

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

let parse<'t> (rule : RuleGroup<'t>) string = 
    let matches target state = 
        let contents = state.Rule.Contents
        let offset = state.Current
        if offset < contents.Length then 
            match contents.[offset] with
            | Rule child when child = target -> true
            | _ -> false
        else false
    
    let states = new List<State<'t>>()
    
    let rec addPattern (pattern : Pattern<'t>) (position : int) (offset : int) (start : int) = 
        if states.[position].Add { Rule = pattern
                                   Start = start
                                   Current = offset }
        then 
            if offset >= pattern.Contents.Length then 
                for parent in states.[start]
                              |> Seq.filter (matches pattern.Group)
                              |> makeList do
                    addPattern parent.Rule position (parent.Current + 1) parent.Start
            else 
                match pattern.Contents.[offset] with
                | Rule group -> addRule group position position
                | Terminal _ -> ()
    
    and addRule (rule : RuleGroup<'t>) (position : int) (start : int) = 
        for rule in rule.Patterns do
            addPattern rule position 0 start
        if rule.IsNullable then 
            for parent in states.[start]
                          |> Seq.filter (matches rule)
                          |> makeList do
                addPattern parent.Rule position (parent.Current + 1) parent.Start
    
    states.Add(new State<'t>())
    addRule rule 0 0
    let mutable position = 0
    for chr in string do
        let currentState = states.[position]
        if currentState.Count > 0 then 
            position <- position + 1
            let nextState = new State<'t>()
            states.Add(nextState)
            for rule in currentState do
                let offset = rule.Current
                let items = rule.Rule
                if offset < items.Contents.Length then 
                    match items.Contents.[offset] with
                    | Terminal term when term.Matches chr -> addPattern items position (offset + 1) rule.Start
                    | Terminal _ -> ()
                    | Rule _ -> ()
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
