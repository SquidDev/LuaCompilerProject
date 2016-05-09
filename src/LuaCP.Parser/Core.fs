namespace LuaCP.Parser

open System
open System.Collections.Generic

/// <summary>
/// A basic predicate. We use this class to allow overriding ToString
/// </summary>
type TerminalPredicate<'t> = 
    interface
        abstract Matches : 't -> bool
    end

type Element<'t> = 
    | Terminal of TerminalPredicate<'t>
    | Rule of RuleGroup<'t>
    override this.ToString() = 
        match this with
        | Terminal x -> x.ToString()
        | Rule child -> child.Name

and RuleGroup<'t>(name : string) = 
    member this.Name = name
    member val internal Patterns = new HashSet<Pattern<'t>>()
    member val internal Nullable = false with get, set
    member this.IsNullable = this.Nullable

    override this.ToString() = 
        sprintf "[Rule %A with %d patterns (nullable: %b)]" name this.Patterns.Count this.Nullable 

and internal Pattern<'t> = 
    { Contents : Element<'t> []
      Group : RuleGroup<'t> }

type Grammar<'t>() = 
    let rules = new HashSet<RuleGroup<'t>>()
    let users = new Dictionary<RuleGroup<'t>, HashSet<RuleGroup<'t>>>()
    let nullable = new HashSet<RuleGroup<'t>>()
    let mutable changed = false
    
    let buildNullable() = 
        let queue = new Queue<RuleGroup<'t>>(nullable)
        while queue.Count > 0 do
            let workSymbol = queue.Dequeue()
            workSymbol.Nullable <- true
            let mutable userStore = null
            if users.TryGetValue(workSymbol, &userStore) then 
                for workRule in userStore do
                    if nullable.Add workRule then queue.Enqueue workRule
    
    member this.MakeRule(name : string) = 
        let rule = new RuleGroup<'t>(name)
        rules.Add rule |> ignore
        rule
    
    member this.Bake() = 
        if changed then 
            changed <- false
            buildNullable()
    
    member this.AddPattern (rule : RuleGroup<'t>) (pattern : Element<'t> []) = 
        if not (rules.Contains rule) then invalidArg "rule" (sprintf "Rule %s is not in this grammar" rule.Name)
        if rule.Patterns.Add { Contents = pattern
                               Group = rule }
        then 
            changed <- true
            if pattern.Length = 0 then nullable.Add rule |> ignore
            for item in pattern do
                match item with
                | Terminal _ -> ()
                | Rule child -> 
                    let mutable userStore = null
                    if not (users.TryGetValue(child, &userStore)) then 
                        userStore <- new HashSet<_>()
                        users.Add(child, userStore)
                    userStore.Add rule |> ignore
