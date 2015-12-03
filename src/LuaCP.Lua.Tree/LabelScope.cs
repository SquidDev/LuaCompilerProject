using System;
using System.Collections.Generic;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.Reporting;

namespace LuaCP.Tree
{
    public sealed class LabelScope
    {
        private class LabelNode
        {
            public readonly Block Block;
            public bool Declared;
            public List<INode> Users = new List<INode>();

            public LabelNode(Block block, bool declared)
            {
                Block = block;
                Declared = declared; 
            }
        }

        private readonly Dictionary<string, LabelNode> labels = new Dictionary<string, LabelNode>();
        private readonly List<LabelScope> children = new List<LabelScope>();
        private readonly LabelScope parent;
        private readonly Function function;

        public LabelScope(Function function)
        {
            this.function = function;    		
        }

        public LabelScope(LabelScope parent)
        {
            this.parent = parent;
            function = parent.function;
            parent.children.Add(this);
        }

        public Block Get(string name, INode user)
        {
            LabelScope scope = this;
            LabelNode node;
            while (scope != null)
            {
                if (scope.labels.TryGetValue(name, out node))
                {
                    node.Users.Add(user);
                    return node.Block;
                }
                scope = scope.parent;
            }
            
            Block block = new Block(function);
            node = new LabelNode(block, false);
            node.Users.Add(user);
            labels.Add(name, node);
            return block;
        }

        private void HoistLabels(string name, List<LabelNode> export)
        {
            LabelNode node;
            if (labels.TryGetValue(name, out node)) export.Add(node);
            foreach (LabelScope scope in children)
            {
                scope.HoistLabels(name, export);
            }
        }

        private List<LabelNode> HoistLabels(string name)
        {
            List<LabelNode> export = new List<LabelNode>();
            HoistLabels(name, export);
            return export;
        }

        public void Declare(string name, BlockBuilder builder)
        {
            List<LabelNode> nodes = HoistLabels(name);
            if (nodes.Count > 0)
            {
                foreach (LabelNode node in nodes)
                {
                    node.Declared = true;
                    node.Block.AddLast(new Branch(builder.Block));
                }
            }
            else
            {
                labels.Add(name, new LabelNode(builder.Block, true));
            }
        }

        public void Validate()
        {
            IReporter reporter = function.Module.Reporter;
            foreach (KeyValuePair<string, LabelNode> node in labels)
            {
                if (!node.Value.Declared)
                {
                    string message = String.Format("Cannot find label '{0}'", node.Key);
                    foreach (INode user in node.Value.Users)
                    {
                        reporter.Report(ReportLevel.Error, message, user.Position);
                    }
                }
            }

            foreach (LabelScope scope in children) scope.Validate();
        }
    }
}
