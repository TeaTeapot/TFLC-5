using System.Text;

namespace TextEditor
{
    public static class AstPrinter
    {
        public static string Print(AstNode root)
        {
            if (root == null) return "(пустое дерево)";
            var sb = new StringBuilder();
            PrintNode(root, sb, prefix: "", isLast: true);
            return sb.ToString();
        }

        private static void PrintNode(AstNode node, StringBuilder sb,
                                      string prefix, bool isLast)
        {
            string connector = isLast ? "└── " : "├── ";
            string childPrefix = isLast ? "    " : "│   ";

            switch (node)
            {
                case WhileNode w:
                    sb.AppendLine($"{prefix}{connector}WhileNode");
                    sb.AppendLine($"{prefix}{childPrefix}├── [condition]");
                    if (w.Condition != null)
                        PrintNode(w.Condition, sb, prefix + childPrefix + "│   ", isLast: true);
                    sb.AppendLine($"{prefix}{childPrefix}└── [body]");
                    if (w.Body != null)
                        PrintNode(w.Body, sb, prefix + childPrefix + "    ", isLast: true);
                    break;

                case UnaryOpNode u:
                    sb.AppendLine($"{prefix}{connector}UnaryOpNode  operator: \"{u.Operator}\"");
                    if (u.Operand != null)
                        PrintNode(u.Operand, sb, prefix + childPrefix, isLast: true);
                    break;

                case BinaryOpNode b:
                    sb.AppendLine($"{prefix}{connector}BinaryOpNode  operator: \"{b.Operator}\"");
                    if (b.Left != null)
                    {
                        sb.AppendLine($"{prefix}{childPrefix}├── [left]");
                        PrintNode(b.Left, sb, prefix + childPrefix + "│   ", isLast: true);
                    }
                    if (b.Right != null)
                    {
                        sb.AppendLine($"{prefix}{childPrefix}└── [right]");
                        PrintNode(b.Right, sb, prefix + childPrefix + "    ", isLast: true);
                    }
                    break;

                case AssignNode a:
                    sb.AppendLine($"{prefix}{connector}AssignNode");
                    if (a.Target != null)
                    {
                        sb.AppendLine($"{prefix}{childPrefix}├── [target]");
                        PrintNode(a.Target, sb, prefix + childPrefix + "│   ", isLast: true);
                    }
                    if (a.Expression != null)
                    {
                        sb.AppendLine($"{prefix}{childPrefix}└── [expression]");
                        PrintNode(a.Expression, sb, prefix + childPrefix + "    ", isLast: true);
                    }
                    break;

                case VariableNode v:
                    string resolvedInfo = v.ResolvedType != "unknown" && v.ResolvedType != null
                        ? $"  : {v.ResolvedType}"
                        : "";
                    sb.AppendLine(
                        $"{prefix}{connector}VariableNode  name: \"{v.Name}\"{resolvedInfo}");
                    break;

                case IntLiteralNode i:
                    sb.AppendLine(
                        $"{prefix}{connector}IntLiteralNode  value: {i.Value}  : Int");
                    break;

                case FloatLiteralNode f:
                    sb.AppendLine(
                        $"{prefix}{connector}FloatLiteralNode  value: {f.Value}  : Float");
                    break;

                case ErrorNode e:
                    sb.AppendLine($"{prefix}{connector}[ErrorNode: {e.Description}]");
                    break;

                default:
                    sb.AppendLine($"{prefix}{connector}{node.NodeType}");
                    break;
            }
        }
    }
}
