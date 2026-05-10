using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TINY_Compiler
{
    // ─────────────────────────────────────────────────────────────
    //  Parse-tree node
    // ─────────────────────────────────────────────────────────────
    public class Node
    {
        public string Name;
        public List<Node> Children = new List<Node>();

        public Node(string name) { Name = name; }

        public void AddChild(Node child)
        {
            if (child != null) Children.Add(child);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Parser  –  top-down recursive-descent for the TINY language
    //
    //  Grammar (from Task 2 document):
    //
    //   1.  Program              → Function_Statements Main_Function
    //   2.  Function_Statements  → Function_Statement Function_Statements | ε
    //   3.  Main_Function        → Datatype main () Function_Body
    //   4.  Function_Statement   → Function_Declaration Function_Body
    //   5.  Function_Body        → { Statements Return_Statement }
    //   6.  Function_Declaration → Datatype FunctionName ( Parameters )
    //   7.  Datatype             → int | float | string
    //   8.  FunctionName         → identifier
    //   9.  Parameters           → Parameter Parameters' | ε
    //  10.  Parameters'          → , Parameter Parameters' | ε
    //  11.  Parameter            → Datatype identifier
    //  12/13. Statements         → Statement Statements | ε
    //  14.  Statement            → Assignment_Statement | Declaration_Statement |
    //                              If_Statement | Repeat_Statement |
    //                              Read_Statement | Write_Statement | Function_Call_Stmt
    //  15.  Assignment_Statement → identifier := Expression
    //  16.  Declaration_Statement→ Datatype VarDeclList ;
    //  17.  VarDeclList          → VarDecl VarDeclList'
    //  18.  VarDeclList'         → , VarDecl VarDeclList' | ε
    //  19.  VarDecl              → identifier Init
    //  20.  Init                 → := Expression | ε
    //  21.  If_Statement         → if Condition_Statement then Statements If_Tail
    //  22.  If_Tail              → Else_If_Statement | Else_Statement | end
    //  23.  Else_If_Statement    → elseif Condition_Statement then Statements If_Tail
    //  24.  Else_Statement       → else Statements end
    //  25.  Repeat_Statement     → repeat Statements until Condition_Statement
    //  26.  Read_Statement       → read identifier ;
    //  27.  Write_Statement      → write Val ;
    //  28.  Val                  → Expression | endl
    //  29.  Function_Call_Stmt   → identifier ( Arguments ) ;
    //  30.  Arguments            → ArgumentList | ε
    //  31.  ArgumentList         → identifier Argument_Tail
    //  32.  Argument_Tail        → , identifier Argument_Tail | ε
    //  33.  Return_Statement     → return Expression ;
    //  34.  Expression           → StringLiteral | Arithmetic_Expression
    //  35.  Arithmetic_Expression→ Term Arithmetic_Expression'
    //  36.  Arithmetic_Expression'→ AddOp Term Arithmetic_Expression' | ε
    //  37.  Term                 → Factor Term'
    //  38.  Term'                → MulOp Factor Term' | ε
    //  39.  Factor               → ( Arithmetic_Expression ) | Value
    //  40.  Value                → number | identifier | Function_Call_Inline
    //       Function_Call_Inline → identifier ( Arguments )        (no semicolon – used inside expressions)
    //  41.  Condition_Statement  → Condition Condition_x
    //  42.  Condition_x          → Boolean_Operator Condition Condition_x | ε
    //  43.  Condition            → identifier Condition_Operator Term
    //  44.  Boolean_Operator     → && | ||
    //  45.  Condition_Operator   → < | > | = | <> | <= | >=
    //  46.  AddOp                → + | -
    //  47.  MulOp                → * | /
    // ─────────────────────────────────────────────────────────────
    public class Parser
    {
        private int _ptr = 0;
        private List<Token> _tokens;

        // ── public entry point ───────────────────────────────────
        public Node StartParsing(List<Token> tokenStream)
        {
            _ptr = 0;
            _tokens = tokenStream;

            Node root = ParseProgram();

            if (_ptr < _tokens.Count)
                AddError($"Unexpected token '{_tokens[_ptr].lex}' after end of program.");
            else
                MessageBox.Show("Parsing completed successfully!", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

            return root;
        }

        // ── helpers ──────────────────────────────────────────────
        private Token Current => (_ptr < _tokens.Count) ? _tokens[_ptr] : null;

        private bool Check(Token_Class tc) =>
            Current != null && Current.token_type == tc;

        private Node Match(Token_Class expected)
        {
            if (Current != null && Current.token_type == expected)
            {
                Node leaf = new Node(Current.lex);
                _ptr++;
                return leaf;
            }

            string found = Current != null ? $"'{Current.lex}'" : "end-of-file";
            AddError($"Expected '{expected}' but found {found}.");
            // panic: skip one token to allow recovery
            if (Current != null) _ptr++;
            return new Node($"<error:{expected}>");
        }

        private void AddError(string msg) =>
            Errors.Error_List.Add("[Parser Error] " + msg);

        // ── FIRST sets (helpers) ─────────────────────────────────
        private bool IsDatatype() =>
            Check(Token_Class.Int) || Check(Token_Class.Float) || Check(Token_Class.StringType);

        private bool IsAddOp() =>
            Check(Token_Class.PlusOp) || Check(Token_Class.MinusOp);

        private bool IsMulOp() =>
            Check(Token_Class.MultiplyOp) || Check(Token_Class.DivideOp);

        private bool IsConditionOp() =>
            Check(Token_Class.LessThanOp) || Check(Token_Class.GreaterThanOp) ||
            Check(Token_Class.EqualOp) || Check(Token_Class.NotEqualOp);

        private bool IsBooleanOp() =>
            Check(Token_Class.AndOp) || Check(Token_Class.OrOp);

        // Returns true when the FIRST(Statement) set is matched,
        // i.e. something that can start a statement.
        private bool IsStatementStart()
        {
            if (Current == null) return false;
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

        // ════════════════════════════════════════════════════════
        //  Rule 1 – Program → Function_Statements Main_Function
        // ════════════════════════════════════════════════════════
        private Node ParseProgram()
        {
            Node n = new Node("Program");
            n.AddChild(ParseFunctionStatements());
            n.AddChild(ParseMainFunction());
            return n;
        }

        // ── Rule 2 – Function_Statements → Function_Statement Function_Statements | ε
        // A Function_Statement starts with a Datatype followed by an identifier that is
        // NOT "main". We peek one token ahead to distinguish from Main_Function.
        private Node ParseFunctionStatements()
        {
            Node n = new Node("Function_Statements");

            while (IsDatatype() && !IsMainNext())
                n.AddChild(ParseFunctionStatement());

            return n;
        }

        // Peek: are we looking at  Datatype "main" ?
        private bool IsMainNext()
        {
            if (!IsDatatype()) return false;
            int next = _ptr + 1;
            return next < _tokens.Count && _tokens[next].token_type == Token_Class.Main;
        }

        // ── Rule 3 – Main_Function → Datatype main () Function_Body
        private Node ParseMainFunction()
        {
            Node n = new Node("Main_Function");
            n.AddChild(ParseDatatype());
            n.AddChild(Match(Token_Class.Main));
            n.AddChild(Match(Token_Class.LParanthesis));
            n.AddChild(Match(Token_Class.RParanthesis));
            n.AddChild(ParseFunctionBody());
            return n;
        }

        // ── Rule 4 – Function_Statement → Function_Declaration Function_Body
        private Node ParseFunctionStatement()
        {
            Node n = new Node("Function_Statement");
            n.AddChild(ParseFunctionDeclaration());
            n.AddChild(ParseFunctionBody());
            return n;
        }

        // ── Rule 5 – Function_Body → { Statements Return_Statement }
        private Node ParseFunctionBody()
        {
            Node n = new Node("Function_Body");
            n.AddChild(Match(Token_Class.FunctionStartOp));
            n.AddChild(ParseStatements());
            n.AddChild(ParseReturnStatement());
            n.AddChild(Match(Token_Class.FunctionEndOp));
            return n;
        }

        // ── Rule 6 – Function_Declaration → Datatype FunctionName ( Parameters )
        private Node ParseFunctionDeclaration()
        {
            Node n = new Node("Function_Declaration");
            n.AddChild(ParseDatatype());
            n.AddChild(ParseFunctionName());
            n.AddChild(Match(Token_Class.LParanthesis));
            n.AddChild(ParseParameters());
            n.AddChild(Match(Token_Class.RParanthesis));
            return n;
        }

        // ── Rule 7 – Datatype → int | float | string
        private Node ParseDatatype()
        {
            Node n = new Node("Datatype");
            if (Check(Token_Class.Int)) n.AddChild(Match(Token_Class.Int));
            else if (Check(Token_Class.Float)) n.AddChild(Match(Token_Class.Float));
            else if (Check(Token_Class.StringType)) n.AddChild(Match(Token_Class.StringType));
            else AddError($"Expected a datatype (int|float|string) but found '{Current?.lex}'.");
            return n;
        }

        // ── Rule 8 – FunctionName → identifier
        private Node ParseFunctionName()
        {
            Node n = new Node("FunctionName");
            n.AddChild(Match(Token_Class.Identifier));
            return n;
        }

        // ── Rules 9-10 – Parameters → Parameter Parameters' | ε
        private Node ParseParameters()
        {
            Node n = new Node("Parameters");
            if (!IsDatatype()) return n;            // ε

            n.AddChild(ParseParameter());
            while (Check(Token_Class.Comma))
            {
                n.AddChild(Match(Token_Class.Comma));
                n.AddChild(ParseParameter());
            }
            return n;
        }

        // ── Rule 11 – Parameter → Datatype identifier
        private Node ParseParameter()
        {
            Node n = new Node("Parameter");
            n.AddChild(ParseDatatype());
            n.AddChild(Match(Token_Class.Identifier));
            return n;
        }

        // ── Rules 12-13 – Statements → Statement Statements | ε
        private Node ParseStatements()
        {
            Node n = new Node("Statements");
            while (IsStatementStart())
                n.AddChild(ParseStatement());
            return n;
        }

        // ── Rule 14 – Statement → one of many alternatives
        // Disambiguation:
        //   Identifier followed by ":="            → Assignment_Statement
        //   Identifier followed by "("             → Function_Call_Stmt
        //   Identifier alone (shouldn't happen standalone, treat as error)
        //   Datatype                               → Declaration_Statement
        //   if                                     → If_Statement
        //   repeat                                 → Repeat_Statement
        //   read                                   → Read_Statement
        //   write                                  → Write_Statement
        private Node ParseStatement()
        {
            Node n = new Node("Statement");

            if (Check(Token_Class.Identifier))
            {
                // peek at the token after the identifier
                int next = _ptr + 1;
                if (next < _tokens.Count && _tokens[next].token_type == Token_Class.AssignmentOp)
                    n.AddChild(ParseAssignmentStatement());
                else
                    n.AddChild(ParseFunctionCallStmt());
            }
            else if (IsDatatype())
                n.AddChild(ParseDeclarationStatement());
            else if (Check(Token_Class.If))
                n.AddChild(ParseIfStatement());
            else if (Check(Token_Class.Repeat))
                n.AddChild(ParseRepeatStatement());
            else if (Check(Token_Class.Read))
                n.AddChild(ParseReadStatement());
            else if (Check(Token_Class.Write))
                n.AddChild(ParseWriteStatement());
            else
            {
                AddError($"Unexpected token '{Current?.lex}' at start of statement.");
                _ptr++; // recover
            }

            return n;
        }

        // ── Rule 15 – Assignment_Statement → identifier := Expression
        private Node ParseAssignmentStatement()
        {
            Node n = new Node("Assignment_Statement");
            n.AddChild(Match(Token_Class.Identifier));
            n.AddChild(Match(Token_Class.AssignmentOp));
            n.AddChild(ParseExpression());
            return n;
        }

        // ── Rule 16 – Declaration_Statement → Datatype VarDeclList ;
        private Node ParseDeclarationStatement()
        {
            Node n = new Node("Declaration_Statement");
            n.AddChild(ParseDatatype());
            n.AddChild(ParseVarDeclList());
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        // ── Rules 17-18 – VarDeclList → VarDecl VarDeclList'
        private Node ParseVarDeclList()
        {
            Node n = new Node("VarDeclList");
            n.AddChild(ParseVarDecl());
            while (Check(Token_Class.Comma))
            {
                n.AddChild(Match(Token_Class.Comma));
                n.AddChild(ParseVarDecl());
            }
            return n;
        }

        // ── Rule 19-20 – VarDecl → identifier Init
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

        // ── Rule 21 – If_Statement → if Condition_Statement then Statements If_Tail
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

        // ── Rule 22 – If_Tail → Else_If_Statement | Else_Statement | end
        private Node ParseIfTail()
        {
            Node n = new Node("If_Tail");
            if (Check(Token_Class.ElseIf))
                n.AddChild(ParseElseIfStatement());
            else if (Check(Token_Class.Else))
                n.AddChild(ParseElseStatement());
            else
                n.AddChild(Match(Token_Class.End));
            return n;
        }

        // ── Rule 23 – Else_If_Statement → elseif Condition_Statement then Statements If_Tail
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

        // ── Rule 24 – Else_Statement → else Statements end
        private Node ParseElseStatement()
        {
            Node n = new Node("Else_Statement");
            n.AddChild(Match(Token_Class.Else));
            n.AddChild(ParseStatements());
            n.AddChild(Match(Token_Class.End));
            return n;
        }

        // ── Rule 25 – Repeat_Statement → repeat Statements until Condition_Statement
        private Node ParseRepeatStatement()
        {
            Node n = new Node("Repeat_Statement");
            n.AddChild(Match(Token_Class.Repeat));
            n.AddChild(ParseStatements());
            n.AddChild(Match(Token_Class.Until));
            n.AddChild(ParseConditionStatement());
            return n;
        }

        // ── Rule 26 – Read_Statement → read identifier ;
        private Node ParseReadStatement()
        {
            Node n = new Node("Read_Statement");
            n.AddChild(Match(Token_Class.Read));
            n.AddChild(Match(Token_Class.Identifier));
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        // ── Rule 27-28 – Write_Statement → write Val ;
        private Node ParseWriteStatement()
        {
            Node n = new Node("Write_Statement");
            n.AddChild(Match(Token_Class.Write));
            n.AddChild(ParseVal());
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        // ── Rule 28 – Val → Expression | endl
        private Node ParseVal()
        {
            Node n = new Node("Val");
            if (Check(Token_Class.Endl))
                n.AddChild(Match(Token_Class.Endl));
            else
                n.AddChild(ParseExpression());
            return n;
        }

        // ── Rule 29 – Function_Call_Stmt → identifier ( Arguments ) ;
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

        // ── Rules 30-32 – Arguments → ArgumentList | ε
        private Node ParseArguments()
        {
            Node n = new Node("Arguments");
            if (!Check(Token_Class.Identifier)) return n;   // ε

            n.AddChild(Match(Token_Class.Identifier));
            while (Check(Token_Class.Comma))
            {
                n.AddChild(Match(Token_Class.Comma));
                n.AddChild(Match(Token_Class.Identifier));
            }
            return n;
        }

        // ── Rule 33 – Return_Statement → return Expression ;
        private Node ParseReturnStatement()
        {
            Node n = new Node("Return_Statement");
            n.AddChild(Match(Token_Class.Return));
            n.AddChild(ParseExpression());
            n.AddChild(Match(Token_Class.Semicolon));
            return n;
        }

        // ── Rule 34 – Expression → StringLiteral | Arithmetic_Expression
        private Node ParseExpression()
        {
            Node n = new Node("Expression");
            if (Check(Token_Class.StringLiteral))
                n.AddChild(Match(Token_Class.StringLiteral));
            else
                n.AddChild(ParseArithmeticExpression());
            return n;
        }

        // ── Rules 35-36 – Arithmetic_Expression → Term Arithmetic_Expression'
        private Node ParseArithmeticExpression()
        {
            Node n = new Node("Arithmetic_Expression");
            n.AddChild(ParseTerm());
            while (IsAddOp())
            {
                n.AddChild(ParseAddOp());
                n.AddChild(ParseTerm());
            }
            return n;
        }

        // ── Rules 37-38 – Term → Factor Term'
        private Node ParseTerm()
        {
            Node n = new Node("Term");
            n.AddChild(ParseFactor());
            while (IsMulOp())
            {
                n.AddChild(ParseMulOp());
                n.AddChild(ParseFactor());
            }
            return n;
        }

        // ── Rule 39 – Factor → ( Arithmetic_Expression ) | Value
        private Node ParseFactor()
        {
            Node n = new Node("Factor");
            if (Check(Token_Class.LParanthesis))
            {
                n.AddChild(Match(Token_Class.LParanthesis));
                n.AddChild(ParseArithmeticExpression());
                n.AddChild(Match(Token_Class.RParanthesis));
            }
            else
                n.AddChild(ParseValue());
            return n;
        }

        // ── Rule 40 – Value → number | identifier [( Arguments )]
        //  If an identifier is followed by "(", it's a Function_Call_Inline.
        private Node ParseValue()
        {
            Node n = new Node("Value");
            if (Check(Token_Class.Constant))
            {
                n.AddChild(Match(Token_Class.Constant));
            }
            else if (Check(Token_Class.Identifier))
            {
                int next = _ptr + 1;
                if (next < _tokens.Count && _tokens[next].token_type == Token_Class.LParanthesis)
                {
                    // Function_Call_Inline (no semicolon)
                    Node fc = new Node("Function_Call_Inline");
                    fc.AddChild(Match(Token_Class.Identifier));
                    fc.AddChild(Match(Token_Class.LParanthesis));
                    fc.AddChild(ParseArguments());
                    fc.AddChild(Match(Token_Class.RParanthesis));
                    n.AddChild(fc);
                }
                else
                    n.AddChild(Match(Token_Class.Identifier));
            }
            else
                AddError($"Expected a value (number or identifier) but found '{Current?.lex}'.");
            return n;
        }

        // ── Rules 41-42 – Condition_Statement → Condition Condition_x
        private Node ParseConditionStatement()
        {
            Node n = new Node("Condition_Statement");
            n.AddChild(ParseCondition());
            while (IsBooleanOp())
            {
                n.AddChild(ParseBooleanOp());
                n.AddChild(ParseCondition());
            }
            return n;
        }

        // ── Rule 43 – Condition → identifier Condition_Operator Term
        private Node ParseCondition()
        {
            Node n = new Node("Condition");
            n.AddChild(Match(Token_Class.Identifier));
            n.AddChild(ParseConditionOperator());
            n.AddChild(ParseTerm());
            return n;
        }

        // ── Rule 44 – Boolean_Operator → && | ||
        private Node ParseBooleanOp()
        {
            Node n = new Node("Boolean_Operator");
            if (Check(Token_Class.AndOp)) n.AddChild(Match(Token_Class.AndOp));
            else if (Check(Token_Class.OrOp)) n.AddChild(Match(Token_Class.OrOp));
            else AddError($"Expected boolean operator (&&/||) but found '{Current?.lex}'.");
            return n;
        }

        // ── Rule 45 – Condition_Operator → < | > | = | <> | <= | >=
        private Node ParseConditionOperator()
        {
            Node n = new Node("Condition_Operator");
            if (Check(Token_Class.LessThanOp)) n.AddChild(Match(Token_Class.LessThanOp));
            else if (Check(Token_Class.GreaterThanOp)) n.AddChild(Match(Token_Class.GreaterThanOp));
            else if (Check(Token_Class.EqualOp)) n.AddChild(Match(Token_Class.EqualOp));
            else if (Check(Token_Class.NotEqualOp)) n.AddChild(Match(Token_Class.NotEqualOp));
            else AddError($"Expected condition operator but found '{Current?.lex}'.");
            return n;
        }

        // ── Rule 46
        private Node ParseAddOp()
        {
            Node n = new Node("AddOp");
            if (Check(Token_Class.PlusOp)) n.AddChild(Match(Token_Class.PlusOp));
            else if (Check(Token_Class.MinusOp)) n.AddChild(Match(Token_Class.MinusOp));
            return n;
        }

        // ── Rule 47
        private Node ParseMulOp()
        {
            Node n = new Node("MulOp");
            if (Check(Token_Class.MultiplyOp)) n.AddChild(Match(Token_Class.MultiplyOp));
            else if (Check(Token_Class.DivideOp)) n.AddChild(Match(Token_Class.DivideOp));
            return n;
        }

        // ════════════════════════════════════════════════════════
        //  Parse-tree → WinForms TreeView helper
        // ════════════════════════════════════════════════════════
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