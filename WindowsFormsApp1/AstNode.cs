using System.Collections.Generic;

namespace TextEditor
{
    public abstract class AstNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public abstract string NodeType { get; }
    }

    public class WhileNode : AstNode
    {
        public override string NodeType => "WhileNode";
        public AstNode Condition { get; set; }
        public AstNode Body { get; set; }
    }

    public class UnaryOpNode : AstNode
    {
        public override string NodeType => "UnaryOpNode";
        public string Operator { get; set; }   
        public AstNode Operand { get; set; }
    }

    public class BinaryOpNode : AstNode
    {
        public override string NodeType => "BinaryOpNode";
        public string Operator { get; set; }  
        public AstNode Left { get; set; }
        public AstNode Right { get; set; }
    }

    public class AssignNode : AstNode
    {
        public override string NodeType => "AssignNode";
        public VariableNode Target { get; set; }
        public AstNode Expression { get; set; }
    }

    public class VariableNode : AstNode
    {
        public override string NodeType => "VariableNode";
        public string Name { get; set; }
        public string ResolvedType { get; set; } = "unknown";
    }

    public class IntLiteralNode : AstNode
    {
        public override string NodeType => "IntLiteralNode";
        public long Value { get; set; }
        public string ResolvedType => "Int";
    }

    public class FloatLiteralNode : AstNode
    {
        public override string NodeType => "FloatLiteralNode";
        public double Value { get; set; }
        public string ResolvedType => "Float";
    }

    public class ErrorNode : AstNode
    {
        public override string NodeType => "ErrorNode";
        public string Description { get; set; }
    }
}
