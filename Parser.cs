using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TINY_Compiler
{
    // ─────────────────────────────────────────────────────────────
    // Parse Tree Node
    // ─────────────────────────────────────────────────────────────
    public class Node
    {
        public string Name;
        public List<Node> Children = new List<Node>();

        public Node(string name)
        {
            Name = name;
        }

        public void AddChild(Node child)
        {
            if (child != null)
                Children.Add(child);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Parser
    // ─────────────────────────────────────────────────────────────
    public class Parser
    {
        private int _ptr = 0;
        private List<Token> _tokens;
        private bool _errorOccurred = false;

        // =========================================================
        // ENTRY
        // =========================================================
        public Node StartParsing(List<Token> tokenStream)
        {
            _ptr = 0;
            _tokens = tokenStream;
            _errorOccurred = false;

            Node root = ParseProgram();

            // Check if we hit an error during the recursive descent
            if (_errorOccurred)
            {
                return null; // Return null so the UI doesn't draw a tree
            }

            // Check for trailing tokens
            if (_ptr < _tokens.Count)
            {
                AddError($"Unexpected token '{_tokens[_ptr].lex}' after end of program.");
                return null;
            }

            MessageBox.Show(
                "Parsing completed successfully!",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            return root;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private Token Current =>
            (_ptr < _tokens.Count) ? _tokens[_ptr] : null;

        private bool Check(Token_Class tc)
        {
            if (_errorOccurred) return false;
            return Current != null && Current.token_type == tc;
        }

        private Node Match(Token_Class expected)
        {
            if (_errorOccurred) return null;

            if (Current != null && Current.token_type == expected)
            {
                Node leaf = new Node(Current.lex);
                _ptr++;
                return leaf;
            }

            string found = Current != null ? $"'{Current.lex}'" : "EOF";
            AddError($"Expected '{expected}' but found {found}");
            return null;
        }

        private void AddError(string msg)
        {
            if (!_errorOccurred) // Only show the first breaking error or collect all
            {
                Errors.Error_List.Add("[Parser Error] " + msg);
                _errorOccurred = true;
            }
        }

        // =========================================================
        // FIRST SET HELPERS
        // =========================================================

        private bool IsDatatype() => Check(Token_Class.Int) || Check(Token_Class.Float) || Check(Token_Class.StringType);
        private bool IsAddOp() => Check(Token_Class.PlusOp) || Check(Token_Class.MinusOp);
        private bool IsMulOp() => Check(Token_Class.MultiplyOp) || Check(Token_Class.DivideOp);
        private bool IsExpressionStart() => Check(Token_Class.Identifier) || Check(Token_Class.Constant) || Check(Token_Class.StringLiteral) || Check(Token_Class.LParanthesis) || Check(Token_Class.MinusOp);

        private bool IsStatementStart()
        {
            if (Current == null || _errorOccurred) return false;
            switch (Current.token_type)
            {
                case Token_Class.Identifier:
                case Token_Class.Int:
                case Token_Class.Float:
                case Token_Class.StringType:
                case Token_Class.If:
                case Token_Class.Repeat:
                case Token_Class.Read:
                case Token_Class.Write:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsMainNext()
        {
            if (!IsDatatype()) return false;
            int next = _ptr + 1;
            return next < _tokens.Count && _tokens[next].token_type == Token_Class.Main;
        }

        // =========================================================
        // GRAMMAR RULES
        // =========================================================

        private Node ParseProgram()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Program");
            n.AddChild(ParseFunctionStatements());
            n.AddChild(ParseMainFunction());
            return n;
        }

        private Node ParseFunctionStatements()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Function_Statements");
            while (IsDatatype() && !IsMainNext() && !_errorOccurred)
            {
                n.AddChild(ParseFunctionStatement());
            }
            return n;
        }

        private Node ParseFunctionStatement()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Function_Statement");
            n.AddChild(ParseFunctionDeclaration());
            n.AddChild(ParseFunctionBody());
            return n;
        }

        private Node ParseMainFunction()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Main_Function");
            n.AddChild(ParseDatatype());
            n.AddChild(Match(Token_Class.Main));
            n.AddChild(Match(Token_Class.LParanthesis));
            n.AddChild(Match(Token_Class.RParanthesis));
            n.AddChild(ParseFunctionBody());
            return n;
        }

        private Node ParseFunctionDeclaration()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Function_Declaration");
            n.AddChild(ParseDatatype());
            n.AddChild(ParseFunctionName());
            n.AddChild(Match(Token_Class.LParanthesis));
            n.AddChild(ParseParameters());
            n.AddChild(Match(Token_Class.RParanthesis));
            return n;
        }

        private Node ParseFunctionBody()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Function_Body");
            n.AddChild(Match(Token_Class.FunctionStartOp));
            n.AddChild(ParseStatements());
            n.AddChild(ParseReturnStatement());
            n.AddChild(Match(Token_Class.FunctionEndOp));
            return n;
        }

        private Node ParseFunctionName()
        {
            Node n = new Node("FunctionName");
            n.AddChild(Match(Token_Class.Identifier));
            return n;
        }

        private Node ParseParameters()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Parameters");
            if (!IsDatatype()) return n;
            n.AddChild(ParseParameter());
            while (Check(Token_Class.Comma) && !_errorOccurred)
            {
                n.AddChild(Match(Token_Class.Comma));
                n.AddChild(ParseParameter());
            }
            return n;
        }

        private Node ParseParameter()
        {
            Node n = new Node("Parameter");
            n.AddChild(ParseDatatype());
            n.AddChild(Match(Token_Class.Identifier));
            return n;
        }

        private Node ParseDatatype()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Datatype");
            if (Check(Token_Class.Int)) n.AddChild(Match(Token_Class.Int));
            else if (Check(Token_Class.Float)) n.AddChild(Match(Token_Class.Float));
            else if (Check(Token_Class.StringType)) n.AddChild(Match(Token_Class.StringType));
            else AddError("Expected datatype");
            return n;
        }

        private Node ParseStatements()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Statements");
            while (IsStatementStart() && !_errorOccurred)
            {
                n.AddChild(ParseStatement());
            }
            return n;
        }

        private Node ParseStatement()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Statement");
            if (Check(Token_Class.Identifier))
            {
                int next = _ptr + 1;
                if (next < _tokens.Count)
                {
                    if (_tokens[next].token_type == Token_Class.AssignmentOp)
                        n.AddChild(ParseAssignmentStatement());
                    else if (_tokens[next].token_type == Token_Class.LParanthesis)
                        n.AddChild(ParseFunctionCallStmt());
                    else
                        AddError($"Invalid statement sequence after '{Current.lex}'");
                }
            }
            else if (IsDatatype()) n.AddChild(ParseDeclarationStatement());
            else if (Check(Token_Class.If)) n.AddChild(ParseIfStatement());
            else if (Check(Token_Class.Repeat)) n.AddChild(ParseRepeatStatement());
            else if (Check(Token_Class.Read)) n.AddChild(ParseReadStatement());
            else if (Check(Token_Class.Write)) n.AddChild(ParseWriteStatement());
            else AddError($"Unexpected token '{Current?.lex}'");
            return n;
        }

        private Node ParseAssignmentStatement()
        {
            Node n = new Node("Assignment_Statement");
            n.AddChild(Match(Token_Class.Identifier));
            n.AddChild(Match(Token_Class.AssignmentOp));
            n.AddChild(ParseExpression());
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        private Node ParseDeclarationStatement()
        {
            Node n = new Node("Declaration_Statement");
            n.AddChild(ParseDatatype());
            n.AddChild(ParseVarDeclList());
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        private Node ParseVarDeclList()
        {
            Node n = new Node("VarDeclList");
            n.AddChild(ParseVarDecl());
            while (Check(Token_Class.Comma) && !_errorOccurred)
            {
                n.AddChild(Match(Token_Class.Comma));
                n.AddChild(ParseVarDecl());
            }
            return n;
        }

        private Node ParseVarDecl()
        {
            Node n = new Node("VarDecl");
            n.AddChild(Match(Token_Class.Identifier));
            if (Check(Token_Class.AssignmentOp))
            {
                n.AddChild(Match(Token_Class.AssignmentOp));
                n.AddChild(ParseExpression());
            }
            return n;
        }

        private Node ParseIfStatement()
        {
            Node n = new Node("If_Statement");
            n.AddChild(Match(Token_Class.If));
            n.AddChild(ParseConditionStatement());
            n.AddChild(Match(Token_Class.Then));
            n.AddChild(ParseStatements());
            n.AddChild(ParseIfTail());
            return n;
        }

        private Node ParseIfTail()
        {
            if (_errorOccurred) return null;
            Node n = new Node("If_Tail");
            if (Check(Token_Class.ElseIf)) n.AddChild(ParseElseIfStatement());
            else if (Check(Token_Class.Else)) n.AddChild(ParseElseStatement());
            else n.AddChild(Match(Token_Class.End));
            return n;
        }

        private Node ParseElseIfStatement()
        {
            Node n = new Node("Else_If_Statement");
            n.AddChild(Match(Token_Class.ElseIf));
            n.AddChild(ParseConditionStatement());
            n.AddChild(Match(Token_Class.Then));
            n.AddChild(ParseStatements());
            n.AddChild(ParseIfTail());
            return n;
        }

        private Node ParseElseStatement()
        {
            Node n = new Node("Else_Statement");
            n.AddChild(Match(Token_Class.Else));
            n.AddChild(ParseStatements());
            n.AddChild(Match(Token_Class.End));
            return n;
        }

        private Node ParseRepeatStatement()
        {
            Node n = new Node("Repeat_Statement");
            n.AddChild(Match(Token_Class.Repeat));
            n.AddChild(ParseStatements());
            n.AddChild(Match(Token_Class.Until));
            n.AddChild(ParseConditionStatement());
            return n;
        }

        private Node ParseReadStatement()
        {
            Node n = new Node("Read_Statement");
            n.AddChild(Match(Token_Class.Read));
            n.AddChild(Match(Token_Class.Identifier));
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        private Node ParseWriteStatement()
        {
            Node n = new Node("Write_Statement");
            n.AddChild(Match(Token_Class.Write));
            n.AddChild(ParseVal());
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        private Node ParseVal()
        {
            Node n = new Node("Val");
            if (Check(Token_Class.Endl)) n.AddChild(Match(Token_Class.Endl));
            else n.AddChild(ParseExpression());
            return n;
        }

        private Node ParseReturnStatement()
        {
            Node n = new Node("Return_Statement");
            n.AddChild(Match(Token_Class.Return));
            n.AddChild(ParseExpression());
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        private Node ParseFunctionCallStmt()
        {
            Node n = new Node("Function_Call_Stmt");
            n.AddChild(Match(Token_Class.Identifier));
            n.AddChild(Match(Token_Class.LParanthesis));
            n.AddChild(ParseArguments());
            n.AddChild(Match(Token_Class.RParanthesis));
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        private Node ParseArguments()
        {
            Node n = new Node("Arguments");
            if (IsExpressionStart())
            {
                n.AddChild(ParseExpression());
                while (Check(Token_Class.Comma) && !_errorOccurred)
                {
                    n.AddChild(Match(Token_Class.Comma));
                    n.AddChild(ParseExpression());
                }
            }
            return n;
        }

        private Node ParseExpression()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Expression");
            if (Check(Token_Class.StringLiteral)) n.AddChild(Match(Token_Class.StringLiteral));
            else n.AddChild(ParseArithmeticExpression());
            return n;
        }

        private Node ParseArithmeticExpression()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Arithmetic_Expression");
            n.AddChild(ParseTerm());
            while (IsAddOp() && !_errorOccurred)
            {
                n.AddChild(ParseAddOp());
                n.AddChild(ParseTerm());
            }
            return n;
        }

        private Node ParseTerm()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Term");
            n.AddChild(ParseFactor());
            while (IsMulOp() && !_errorOccurred)
            {
                n.AddChild(ParseMulOp());
                n.AddChild(ParseFactor());
            }
            return n;
        }

        private Node ParseFactor()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Factor");
            if (Check(Token_Class.MinusOp))
            {
                n.AddChild(Match(Token_Class.MinusOp));
                n.AddChild(ParseFactor());
            }
            else if (Check(Token_Class.LParanthesis))
            {
                n.AddChild(Match(Token_Class.LParanthesis));
                n.AddChild(ParseArithmeticExpression());
                n.AddChild(Match(Token_Class.RParanthesis));
            }
            else n.AddChild(ParseValue());
            return n;
        }

        private Node ParseValue()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Value");
            if (Check(Token_Class.Constant)) n.AddChild(Match(Token_Class.Constant));
            else if (Check(Token_Class.Identifier))
            {
                int next = _ptr + 1;
                if (next < _tokens.Count && _tokens[next].token_type == Token_Class.LParanthesis)
                {
                    Node fc = new Node("Function_Call_Inline");
                    fc.AddChild(Match(Token_Class.Identifier));
                    fc.AddChild(Match(Token_Class.LParanthesis));
                    fc.AddChild(ParseArguments());
                    fc.AddChild(Match(Token_Class.RParanthesis));
                    n.AddChild(fc);
                }
                else n.AddChild(Match(Token_Class.Identifier));
            }
            else AddError($"Expected value near '{Current?.lex}'");
            return n;
        }

        private Node ParseConditionStatement()
        {
            return ParseOrCondition();
        }

        private Node ParseOrCondition()
        {
            if (_errorOccurred) return null;
            Node n = new Node("OrCondition");
            n.AddChild(ParseAndCondition());
            while (Check(Token_Class.OrOp) && !_errorOccurred)
            {
                n.AddChild(Match(Token_Class.OrOp));
                n.AddChild(ParseAndCondition());
            }
            return n;
        }

        private Node ParseAndCondition()
        {
            if (_errorOccurred) return null;
            Node n = new Node("AndCondition");
            n.AddChild(ParseCondition());
            while (Check(Token_Class.AndOp) && !_errorOccurred)
            {
                n.AddChild(Match(Token_Class.AndOp));
                n.AddChild(ParseCondition());
            }
            return n;
        }

        private Node ParseCondition()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Condition");
            n.AddChild(ParseExpression());
            n.AddChild(ParseConditionOperator());
            n.AddChild(ParseExpression());
            return n;
        }

        private Node ParseConditionOperator()
        {
            if (_errorOccurred) return null;
            Node n = new Node("Condition_Operator");
            if (Check(Token_Class.LessThanOp)) n.AddChild(Match(Token_Class.LessThanOp));
            else if (Check(Token_Class.GreaterThanOp)) n.AddChild(Match(Token_Class.GreaterThanOp));
            else if (Check(Token_Class.EqualOp)) n.AddChild(Match(Token_Class.EqualOp));
            else if (Check(Token_Class.NotEqualOp)) n.AddChild(Match(Token_Class.NotEqualOp));
            else if (Check(Token_Class.LessThanOrEqualOp)) n.AddChild(Match(Token_Class.LessThanOrEqualOp));
            else if (Check(Token_Class.GreaterThanOrEqualOp)) n.AddChild(Match(Token_Class.GreaterThanOrEqualOp));
            else AddError("Expected condition operator");
            return n;
        }

        private Node ParseAddOp()
        {
            Node n = new Node("AddOp");
            if (Check(Token_Class.PlusOp)) n.AddChild(Match(Token_Class.PlusOp));
            else n.AddChild(Match(Token_Class.MinusOp));
            return n;
        }

        private Node ParseMulOp()
        {
            Node n = new Node("MulOp");
            if (Check(Token_Class.MultiplyOp)) n.AddChild(Match(Token_Class.MultiplyOp));
            else n.AddChild(Match(Token_Class.DivideOp));
            return n;
        }

        public static TreeNode ToTreeView(Node root)
        {
            if (root == null) return null;
            TreeNode tv = new TreeNode(root.Name);
            foreach (Node child in root.Children)
            {
                TreeNode tvChild = ToTreeView(child);
                if (tvChild != null) tv.Nodes.Add(tvChild);
            }
            return tv;
        }
    }
}