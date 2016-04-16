module LuaCP.Parser.Nodes

open LuaCP.Tree
open LuaCP.Tree.Expression
open LuaCP.Lua.Tree
open LuaCP.Lua.Tree.Statement
open LuaCP.Lua.Tree.Expression
open LuaCP.IR
open LuaCP.IR.Instructions

let Nil : IValueNode = upcast ConstantNode.Nil
let String x : IValueNode = upcast new ConstantNode(new Literal.String(x))
let Number x : IValueNode = upcast new ConstantNode(new Literal.Number(x))
let Integer x : IValueNode = upcast new ConstantNode(new Literal.Integer(x))

let ChooseNumber(x : double) : IValueNode = 
    if (x % 1.0) = 0.0 then Integer(int x)
    else Number x

let Boolean x : IValueNode = upcast new ConstantNode(new Literal.Boolean(x))
let Table x : IValueNode = upcast new TableNode(x)
let TableItem x = new TableNode.TableItem(x)
let TablePair x y = new TableNode.TableItem(x, y)
let Dots : IValueNode = upcast new DotsNode()
let Parens x : IValueNode = upcast new ParenthesisNode(x)
let BinOp op left right : IValueNode = upcast new BinOpNode(op, left, right)
let UnOp op operand : IValueNode = upcast new UnaryOpNode(op, operand)
let Index tbl key = new IndexNode(tbl, key)
let Identifier name = new IdentifierNode(name)
let Function args dots body : IValueNode = upcast new FunctionNode(args, dots, body)
let Call func args : IValueNode = upcast new CallNode(func, args)
let Invoke tbl name args : IValueNode = upcast new CallNode(Index tbl (String name), tbl :: args)
let Block x : INode = upcast new BlockNode(x)
let Local declared values : INode = upcast new LocalNode(declared, values)
let Assign assignable values : INode = upcast new AssignNode(assignable, values)

let LocalRec declared values : INode = 
    Block [ Local declared (List.replicate (Seq.length declared) Nil)
            Assign (Seq.cast<IAssignable> declared) values ]

let Do x : INode = upcast new DoNode(x)
let If condition yes no : INode = upcast new IfNode(condition, yes, no)
let While condition block : INode = upcast new WhileNode(condition, block)
let Return nodes : INode = upcast new ReturnNode(nodes)
let Break : INode = upcast new BreakNode()
let Repeat block condition : INode = upcast new RepeatNode(block, condition)
let ForIn idents (generator : list<IValueNode>) block : INode = upcast new ForInNode(generator, idents, block)
let ForNum ident start stop step block : INode = upcast new ForNumNode(ident, start, stop, step, block)
let Goto x : INode = upcast new GoToNode(x)
let Label x : INode = upcast new LabelNode(x)

let IfElseIf condition body elseifs (elses : option<INode>) : INode = 
    let rec convert elseifs elses = 
        match elseifs, elses with
        | [], None -> Block []
        | [], Some x -> x
        // TODO: Actually tail recursive
        | (condition, body) :: remainder, elses -> upcast new IfNode(condition, body, convert remainder elses)
    upcast new IfNode(condition, body, convert elseifs elses)