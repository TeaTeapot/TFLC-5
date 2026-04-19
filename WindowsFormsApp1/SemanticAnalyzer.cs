using System;
using System.Collections.Generic;

namespace TextEditor
{
    public class SemanticAnalyzer
    {
        private readonly SymbolTable _symbols = new SymbolTable();
        private readonly List<SemanticError> _errors = new List<SemanticError>();

        private const long INT_MIN = -2_147_483_648L;
        private const long INT_MAX =  2_147_483_647L;

        public (List<SemanticError> errors, SymbolTable table)
            Analyze(AstNode root,
                    IEnumerable<(string name, string type, string value)> predeclared = null)
        {
            _errors.Clear();
            _symbols.Clear();

            if (predeclared != null)
                foreach (var (name, type, value) in predeclared)
                    _symbols.Declare(name, type, value, line: 0, col: 0);

            if (root != null && !(root is ErrorNode))
                Visit(root);

            return (_errors, _symbols);
        }


        private string Visit(AstNode node)
        {
            switch (node)
            {
                case WhileNode w:    return VisitWhile(w);
                case AssignNode a:   return VisitAssign(a);
                case BinaryOpNode b: return VisitBinaryOp(b);
                case UnaryOpNode u:  return VisitUnaryOp(u);
                case VariableNode v: return VisitVariable(v);
                case IntLiteralNode i:   return VisitIntLiteral(i);
                case FloatLiteralNode f: return VisitFloatLiteral(f);
                case ErrorNode e:    return "error";
                default:             return "unknown";
            }
        }


        private string VisitWhile(WhileNode node)
        {
            string condType = node.Condition != null ? Visit(node.Condition) : "error";
            if (condType != "Bool" && condType != "error")
                AddError(node,
                    $"Условие оператора while должно иметь тип Bool, получен тип '{condType}'");

            if (node.Body != null)
                Visit(node.Body);

            return "void";
        }


        private string VisitAssign(AssignNode node)
        {
            string targetType = VisitVariable(node.Target, mustBeDeclared: true);

            string exprType = node.Expression != null ? Visit(node.Expression) : "error";

            if (targetType != "error" && exprType != "error")
            {
                if (!TypesCompatible(targetType, exprType))
                    AddError(node.Target,
                        $"Правило 2 — несовместимые типы: " +
                        $"переменная '{node.Target.Name}' имеет тип '{targetType}', " +
                        $"но присваивается значение типа '{exprType}'");
            }

            return "void";
        }


        private string VisitBinaryOp(BinaryOpNode node)
        {
            string leftType  = node.Left  != null ? Visit(node.Left)  : "error";
            string rightType = node.Right != null ? Visit(node.Right) : "error";

            if (leftType == "error" || rightType == "error")
                return "error";

            switch (node.Operator)
            {
                case "+": case "-": case "*": case "/": case "%":
                    if (!IsNumeric(leftType))
                        AddError(node, $"Оператор '{node.Operator}': " +
                            $"левый операнд должен быть числовым, получен '{leftType}'");
                    if (!IsNumeric(rightType))
                        AddError(node, $"Оператор '{node.Operator}': " +
                            $"правый операнд должен быть числовым, получен '{rightType}'");
                    return (leftType == "Float" || rightType == "Float") ? "Float" : "Int";

                case ">": case "<": case ">=": case "<=":
                    if (!IsNumeric(leftType))
                        AddError(node, $"Оператор '{node.Operator}': " +
                            $"левый операнд должен быть числовым, получен '{leftType}'");
                    if (!IsNumeric(rightType))
                        AddError(node, $"Оператор '{node.Operator}': " +
                            $"правый операнд должен быть числовым, получен '{rightType}'");
                    return "Bool";

                case "==": case "!=":
                    if (!TypesCompatible(leftType, rightType) &&
                        !TypesCompatible(rightType, leftType))
                        AddError(node, $"Оператор '{node.Operator}': " +
                            $"несравнимые типы '{leftType}' и '{rightType}'");
                    return "Bool";

                case "&&": case "||":
                    if (leftType != "Bool")
                        AddError(node, $"Оператор '{node.Operator}': " +
                            $"левый операнд должен иметь тип Bool, получен '{leftType}'");
                    if (rightType != "Bool")
                        AddError(node, $"Оператор '{node.Operator}': " +
                            $"правый операнд должен иметь тип Bool, получен '{rightType}'");
                    return "Bool";

                default:
                    return "unknown";
            }
        }


        private string VisitUnaryOp(UnaryOpNode node)
        {
            string operandType = node.Operand != null ? Visit(node.Operand) : "error";
            if (operandType == "error") return "error";

            switch (node.Operator)
            {
                case "not":
                    if (operandType != "Bool")
                        AddError(node,
                            $"Оператор 'not' применяется к типу Bool, получен '{operandType}'");
                    return "Bool";

                case "-": case "+":
                    if (!IsNumeric(operandType))
                        AddError(node,
                            $"Унарный '{node.Operator}' применяется к числовому типу, получен '{operandType}'");
                    return operandType;

                default:
                    return operandType;
            }
        }


        private string VisitVariable(AstNode node)
            => VisitVariable((VariableNode)node, mustBeDeclared: false);

        private string VisitVariable(VariableNode node, bool mustBeDeclared = false)
        {
            var entry = _symbols.Lookup(node.Name);

            if (entry == null)
            {
                if (mustBeDeclared)
                {
                    AddError(node,
                        $"Правило 4 — идентификатор '{node.Name}' не объявлен " +
                        $"в текущей области видимости");
                }
                else
                {
                    AddError(node,
                        $"Правило 4 — идентификатор '{node.Name}' используется " +
                        $"до объявления");
                    _symbols.Declare(node.Name, "unknown",
                                     line: node.Line, col: node.Column);
                }
                node.ResolvedType = "error";
                return "error";
            }

            _symbols.MarkUsed(node.Name);
            node.ResolvedType = entry.Type;
            return entry.Type;
        }


        private string VisitIntLiteral(IntLiteralNode node)
        {
            if (node.Value < INT_MIN || node.Value > INT_MAX)
                AddError(node,
                    $"Правило 3 — значение {node.Value} выходит за пределы " +
                    $"типа Int [{INT_MIN}; {INT_MAX}]");
            return "Int";
        }

        private string VisitFloatLiteral(FloatLiteralNode node)
        {
            if (double.IsInfinity(node.Value) || double.IsNaN(node.Value))
                AddError(node,
                    $"Правило 3 — вещественное значение '{node.Value}' недопустимо");
            return "Float";
        }


        private static bool IsNumeric(string type)
            => type == "Int" || type == "Float";

        private static bool TypesCompatible(string targetType, string valueType)
        {
            if (targetType == valueType) return true;
            if (targetType == "Float" && valueType == "Int") return true;
            if (targetType == "unknown" || valueType == "unknown") return true;
            return false;
        }

        private void AddError(AstNode node, string message)
        {
            string location = node.Line > 0
                ? $"строка {node.Line}, символ {node.Column}"
                : "неизвестная позиция";
            _errors.Add(new SemanticError(message, location));
        }
    }
}
