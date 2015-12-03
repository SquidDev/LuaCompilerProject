using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Statement
{
    public class ForNumNode : Node
    {
        public IDeclarable Counter;
        public readonly IValueNode Start;
        public readonly IValueNode End;
        public readonly IValueNode Step;
        public readonly INode Body;

        public ForNumNode(IDeclarable counter, IValueNode start, IValueNode end, IValueNode step, INode body)
        {
            Counter = counter;
            Start = start;
            End = end;
            Step = step;
            Body = body;
        }

        public override BlockBuilder Build(BlockBuilder builder)
        {
            BlockBuilder testBlock = builder.MakeChild();
            BlockBuilder bodyBlock = builder.MakeChild();
            BlockBuilder addBlock = builder.MakeChild();
            BlockBuilder continueBlock = builder.Continue();
            
            // Evaluate values
            IValue start, end, step;
            builder = Start.Build(builder, out start);
            builder = End.Build(builder, out end);
            builder = Step.Build(builder, out step);
            builder.Block.AddLast(new Branch(testBlock.Block));
            
            // Index Phi(Initial => Start, Body => Index + Step)
            Phi index = new Phi(testBlock.Block);
            index.Source.Add(builder.Block, start);

            // if(0 < Step ? Index <= End: End <= Index) { continue; } else { break; }
            // Whilst this is slightly inefficient, it is the only way to do it.
            // However, this should be optimised out most of the time as the step is a constant
            BlockBuilder lessThan = builder.Continue();
            {
                IValue op = lessThan.Block.AddLast(new BinaryOp(Opcode.LessThan, index, end));
                lessThan.Block.AddLast(new BranchCondition(op, bodyBlock.Block, continueBlock.Block));
            }
            BlockBuilder greaterThan = builder.Continue();
            {
                IValue op = greaterThan.Block.AddLast(new BinaryOp(Opcode.LessThan, end, index));
                greaterThan.Block.AddLast(new BranchCondition(op, bodyBlock.Block, continueBlock.Block));
            }
            IValue zeroCheck = testBlock.Block.AddLast(new BinaryOp(Opcode.LessThan, builder.Constants[0], step));
            testBlock.Block.AddLast(new BranchCondition(zeroCheck, lessThan.Block, greaterThan.Block));
        	
            // <Counter> = Index
            Counter.Declare(bodyBlock, index);
            bodyBlock = Body.Build(bodyBlock);
            if (bodyBlock.Block.Count == 0 || !bodyBlock.Block.Last.Opcode.IsTerminator())
            {
                bodyBlock.Block.AddLast(new Branch(addBlock.Block));
            }
            
            // Index += Step
            IValue newIndex = addBlock.Block.AddLast(new BinaryOp(Opcode.Add, start, step));
            index.Source.Add(addBlock.Block, newIndex);
            addBlock.Block.AddLast(new Branch(testBlock.Block));

            return continueBlock;
        }
    }
}

