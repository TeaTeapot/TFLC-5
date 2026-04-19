using System;
using System.Collections.Generic;

namespace TextEditor
{
    public class Parser
    {
        private List<Token> _tokens;
        private int _pos;
        private List<SyntaxError> _errors;
        private string _sourceText;

        private static readonly HashSet<int> ConditionStartTokens = new HashSet<int>
        {
            TokenCodes.Not, TokenCodes.ID, TokenCodes.LeftParen,
            TokenCodes.Num, TokenCodes.Float
        };

        public Parser() { }

        public (AstNode root, List<SyntaxError> errors) Parse(
            List<Token> tokens, string sourceText)
        {
            _tokens     = tokens;
            _sourceText = sourceText;
            _pos        = 0;
            _errors     = new List<SyntaxError>();
            AstNode root = null;

            try
            {
                if (_tokens == null || _tokens.Count == 0)
                {
                    AddError("EOF", "Пустая строка. Ожидалось ключевое слово 'while'");
                    return (null, _errors);
                }

                root = ParseWhileLoop();

                if (_pos < _tokens.Count)
                {
                    var t = Peek();
                    AddError(t.Value,
                        $"Неожиданная лексема '{t.Value}' после завершения цикла.");
                }
            }
            catch (Exception ex)
            {
                _errors.Add(new SyntaxError("Критическая ошибка", "", ex.Message, -1, -1, -1));
            }

            return (root, _errors);
        }


        private WhileNode ParseWhileLoop()
        {
            var node = new WhileNode();

            if (Match(TokenCodes.WhileKeyword))
            { SetPos(node, Peek()); Advance(); }
            else
            {
                AddError(PeekValue(), $"Ожидалось 'while', найдено '{PeekValue()}'");
                if (!ConditionStartTokens.Contains(CurrentCode()))
                    return node;
            }

            node.Condition = ParseCondition();

            if (Match(TokenCodes.DoKeyword))
            { Advance(); }
            else
            {
                AddError(PeekValue(), $"Ожидалось 'do', найдено '{PeekValue()}'");
                if (DoIsAhead())
                    Synchronize(new HashSet<int> { TokenCodes.DoKeyword });
                else
                    if (!IsEnd()) Advance();

                if (Match(TokenCodes.DoKeyword)) Advance();
            }

            node.Body = ParseBody();
            return node;
        }

        private AstNode ParseCondition() => ParseLogicalOrExpr();

        private AstNode ParseLogicalOrExpr()
        {
            var left = ParseLogicalAndExpr();
            while (Match(TokenCodes.Or))
            {
                var op = Peek(); Advance();
                var right = ParseLogicalAndExpr();
                var n = new BinaryOpNode { Operator = "||", Left = left, Right = right };
                SetPos(n, op); left = n;
            }
            return left;
        }

        private AstNode ParseLogicalAndExpr()
        {
            var left = ParseEqualityExpr();
            while (Match(TokenCodes.And))
            {
                var op = Peek(); Advance();
                var right = ParseEqualityExpr();
                var n = new BinaryOpNode { Operator = "&&", Left = left, Right = right };
                SetPos(n, op); left = n;
            }
            return left;
        }

        private AstNode ParseEqualityExpr()
        {
            var left = ParseRelationalExpr();
            while (Match(TokenCodes.Equal) || Match(TokenCodes.NotEqual))
            {
                var op = Peek(); string opStr = op.Value; Advance();
                var right = ParseRelationalExpr();
                var n = new BinaryOpNode { Operator = opStr, Left = left, Right = right };
                SetPos(n, op); left = n;
            }
            return left;
        }

        private AstNode ParseRelationalExpr()
        {
            var left = ParseAdditiveExpr();
            if (Match(TokenCodes.Less) || Match(TokenCodes.Greater) ||
                Match(TokenCodes.LessOrEqual) || Match(TokenCodes.GreaterOrEqual))
            {
                var op = Peek(); string opStr = op.Value; Advance();
                var right = ParseAdditiveExpr();
                var n = new BinaryOpNode { Operator = opStr, Left = left, Right = right };
                SetPos(n, op); return n;
            }
            return left;
        }

        private AstNode ParseAdditiveExpr()
        {
            var left = ParseMultiplicativeExpr();
            while (Match(TokenCodes.Add) || Match(TokenCodes.Sub))
            {
                var op = Peek(); string opStr = op.Value; Advance();
                var right = ParseMultiplicativeExpr();
                var n = new BinaryOpNode { Operator = opStr, Left = left, Right = right };
                SetPos(n, op); left = n;
            }
            return left;
        }

        private AstNode ParseMultiplicativeExpr()
        {
            var left = ParseFactor();
            while (Match(TokenCodes.Mul) || Match(TokenCodes.Div) || Match(TokenCodes.Mod))
            {
                var op = Peek(); string opStr = op.Value; Advance();
                var right = ParseFactor();
                var n = new BinaryOpNode { Operator = opStr, Left = left, Right = right };
                SetPos(n, op); left = n;
            }
            return left;
        }

        private AstNode ParseFactor()
        {
            if (Match(TokenCodes.Not))
            {
                var opT = Peek(); Advance();
                var operand = ParseFactor();
                var n = new UnaryOpNode { Operator = "not", Operand = operand };
                SetPos(n, opT); return n;
            }

            if (Match(TokenCodes.LeftParen))
            {
                Advance();
                var inner = ParseLogicalOrExpr();
                if (Match(TokenCodes.RightParen)) Advance();
                else
                {
                    AddError(PeekValue(), $"Ожидалась ')', найдено '{PeekValue()}'");
                    Synchronize(new HashSet<int>
                        { TokenCodes.RightParen, TokenCodes.Semicolon,
                          TokenCodes.DoKeyword,  TokenCodes.ID });
                    if (Match(TokenCodes.RightParen)) Advance();
                }
                return inner;
            }

            if (Match(TokenCodes.ID))
            {
                var t = Peek();
                var n = new VariableNode { Name = t.Value };
                SetPos(n, t); Advance(); return n;
            }

            if (Match(TokenCodes.Num))
            {
                var t = Peek();
                long.TryParse(t.Value, out long v);
                var n = new IntLiteralNode { Value = v };
                SetPos(n, t); Advance(); return n;
            }

            if (Match(TokenCodes.Float))
            {
                var t = Peek();
                double.TryParse(t.Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double v);
                var n = new FloatLiteralNode { Value = v };
                SetPos(n, t); Advance(); return n;
            }

            AddError(PeekValue(),
                $"Ожидался идентификатор или число, найдено '{PeekValue()}'");
            var err = new ErrorNode { Description = $"unexpected '{PeekValue()}'" };
            Synchronize(new HashSet<int>
            {
                TokenCodes.Add, TokenCodes.Sub, TokenCodes.Mul, TokenCodes.Div,
                TokenCodes.Less, TokenCodes.Greater, TokenCodes.DoKeyword,
                TokenCodes.Semicolon, TokenCodes.RightParen,
                TokenCodes.And, TokenCodes.Or
            });
            return err;
        }


        private AstNode ParseBody() => ParseStatementWithSemicolon();

        private AstNode ParseStatementWithSemicolon()
        {
            var node = ParseStatement();
            if (Match(TokenCodes.Semicolon)) Advance();
            else
            {
                AddError(PeekValue(),
                    $"Ожидался ';' в конце оператора, найдено '{PeekValue()}'");
                Synchronize(new HashSet<int>
                    { TokenCodes.ID, TokenCodes.Semicolon });
                if (Match(TokenCodes.Semicolon)) Advance();
            }
            return node;
        }

        private AstNode ParseStatement()
        {
            VariableNode target;

            if (Match(TokenCodes.ID))
            {
                var t = Peek();
                target = new VariableNode { Name = t.Value };
                SetPos(target, t); Advance();
            }
            else
            {
                AddError(PeekValue(),
                    $"Ожидался идентификатор, найдено '{PeekValue()}'");
                Synchronize(new HashSet<int>
                    { TokenCodes.Assign, TokenCodes.Semicolon });
                target = new VariableNode { Name = "??" };
            }

            if (Match(TokenCodes.Assign)) Advance();
            else
            {
                AddError(PeekValue(),
                    $"Ожидался '<-', найдено '{PeekValue()}'");
                Synchronize(new HashSet<int>
                    { TokenCodes.ID, TokenCodes.Num, TokenCodes.Float,
                      TokenCodes.LeftParen, TokenCodes.Semicolon });
                if (Match(TokenCodes.Assign)) Advance();
            }

            var expr = ParseLogicalOrExpr();
            var assign = new AssignNode { Target = target, Expression = expr };
            SetPos(assign, target.Line, target.Column);
            return assign;
        }


        private bool   Match(int c) => _pos < _tokens.Count && _tokens[_pos].Code == c;
        private Token  Peek()       => _pos < _tokens.Count ? _tokens[_pos] : null;
        private string PeekValue()  => Peek()?.Value ?? "EOF";
        private int    CurrentCode()=> _pos < _tokens.Count ? _tokens[_pos].Code : -999;
        private bool   IsEnd()      => _pos >= _tokens.Count;
        private void   Advance()    { if (!IsEnd()) _pos++; }

        private bool DoIsAhead()
        {
            for (int i = _pos; i < _tokens.Count; i++)
                if (_tokens[i].Code == TokenCodes.DoKeyword) return true;
            return false;
        }

        private void Synchronize(HashSet<int> stopCodes)
        {
            while (_pos < _tokens.Count && !stopCodes.Contains(_tokens[_pos].Code))
                _pos++;
        }

        private void AddError(string fragment, string description)
        {
            var t = Peek();
            string location = t != null ? $"{t.Line}:{t.StartPos}" : "EOF";
            int charPos = t != null ? GetCharPos(t) : -1;
            int line    = t != null ? t.Line : -1;
            _errors.Add(new SyntaxError(fragment, location, description, _pos, charPos, line));
        }

        private int GetCharPos(Token t)
        {
            if (string.IsNullOrEmpty(_sourceText) || t == null) return -1;
            int p = 0;
            string[] lines = _sourceText.Split('\n');
            for (int i = 0; i < t.Line - 1 && i < lines.Length; i++)
                p += lines[i].Length + 1;
            return p + t.StartPos;
        }

        private static void SetPos(AstNode n, Token t)
        {
            if (t == null) return;
            n.Line = t.Line; n.Column = t.StartPos;
        }
        private static void SetPos(AstNode n, int line, int col)
        {
            n.Line = line; n.Column = col;
        }
    }
}
