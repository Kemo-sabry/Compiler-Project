using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TINY_Compiler
{
    public enum Token_Class
    {
        Int, Float, StringType,
        Read, Write,
        Repeat, Until,
        If, ElseIf, Else, Then, End,
        Return, Endl,
        Main,

        Semicolon, Comma,
        LParanthesis, RParanthesis,
        FunctionStartOp, FunctionEndOp,

        EqualOp, LessThanOp, GreaterThanOp,
        LessThanOrEqualOp, GreaterThanOrEqualOp,
        NotEqualOp,

        PlusOp, MinusOp, MultiplyOp, DivideOp,
        AndOp, OrOp,
        AssignmentOp,

        Identifier,
        Constant,
        StringLiteral,

        Undefined
    }

    public class Token
    {
        public string lex;
        public Token_Class token_type;
    }

    public class CompilerState
    {
        public static List<Token> TokenStream = new List<Token>();
    }

    public class Scanner
    {
        public List<Token> Tokens = new List<Token>();

        Dictionary<string, Token_Class> ReservedWords = new Dictionary<string, Token_Class>();
        Dictionary<string, Token_Class> Operators = new Dictionary<string, Token_Class>();

        public Scanner()
        {
            // Reserved words
            ReservedWords["int"] = Token_Class.Int;
            ReservedWords["float"] = Token_Class.Float;
            ReservedWords["string"] = Token_Class.StringType;
            ReservedWords["read"] = Token_Class.Read;
            ReservedWords["write"] = Token_Class.Write;
            ReservedWords["repeat"] = Token_Class.Repeat;
            ReservedWords["until"] = Token_Class.Until;
            ReservedWords["if"] = Token_Class.If;
            ReservedWords["elseif"] = Token_Class.ElseIf;
            ReservedWords["else"] = Token_Class.Else;
            ReservedWords["then"] = Token_Class.Then;
            ReservedWords["end"] = Token_Class.End;
            ReservedWords["return"] = Token_Class.Return;
            ReservedWords["endl"] = Token_Class.Endl;
            ReservedWords["main"] = Token_Class.Main;

            // Operators
            Operators["&&"] = Token_Class.AndOp;
            Operators["||"] = Token_Class.OrOp;

            Operators[":="] = Token_Class.AssignmentOp;

            Operators["<>"] = Token_Class.NotEqualOp;

            Operators["<="] = Token_Class.LessThanOrEqualOp;
            Operators[">="] = Token_Class.GreaterThanOrEqualOp;

            Operators["="] = Token_Class.EqualOp;
            Operators["<"] = Token_Class.LessThanOp;
            Operators[">"] = Token_Class.GreaterThanOp;

            Operators["+"] = Token_Class.PlusOp;
            Operators["-"] = Token_Class.MinusOp;
            Operators["*"] = Token_Class.MultiplyOp;
            Operators["/"] = Token_Class.DivideOp;

            Operators[";"] = Token_Class.Semicolon;
            Operators[","] = Token_Class.Comma;
            Operators["("] = Token_Class.LParanthesis;
            Operators[")"] = Token_Class.RParanthesis;
            Operators["{"] = Token_Class.FunctionStartOp;
            Operators["}"] = Token_Class.FunctionEndOp;
        }

        public void StartScanning(string SourceCode)
        {
            int i = 0;

            // FIX: normalize weird dash from Word/PDF
            SourceCode = SourceCode.Replace('–', '-');

            while (i < SourceCode.Length)
            {
                char CurrentChar = SourceCode[i];

                if (char.IsWhiteSpace(CurrentChar))
                {
                    i++;
                    continue;
                }

                string lexeme = "";

                // IDENTIFIER / KEYWORD
                if (char.IsLetter(CurrentChar))
                {
                    while (i < SourceCode.Length &&
                           char.IsLetterOrDigit(SourceCode[i]))
                    {
                        lexeme += SourceCode[i++];
                    }

                    FindTokenClass(lexeme);
                    continue;
                }

                // NUMBER (int / float)
                else if (char.IsDigit(CurrentChar))
                {
                    bool dotSeen = false;

                    while (i < SourceCode.Length &&
                          (char.IsDigit(SourceCode[i]) ||
                          (!dotSeen && SourceCode[i] == '.')))
                    {
                        if (SourceCode[i] == '.')
                            dotSeen = true;

                        lexeme += SourceCode[i++];
                    }

                    // invalid identifier after number
                    if (i < SourceCode.Length && char.IsLetter(SourceCode[i]))
                    {
                        while (i < SourceCode.Length &&
                               char.IsLetterOrDigit(SourceCode[i]))
                        {
                            lexeme += SourceCode[i++];
                        }

                        Errors.Error_List.Add(
                            $"[Scanner Error] Invalid identifier '{lexeme}'"
                        );
                    }
                    else
                    {
                        FindTokenClass(lexeme);
                    }

                    continue;
                }

                // STRING
                else if (CurrentChar == '"')
                {
                    string str = "\"";
                    i++;

                    while (i < SourceCode.Length &&
                           SourceCode[i] != '"')
                    {
                        str += SourceCode[i++];
                    }

                    if (i < SourceCode.Length)
                    {
                        str += "\"";
                        i++;

                        Tokens.Add(new Token
                        {
                            lex = str,
                            token_type = Token_Class.StringLiteral
                        });
                    }
                    else
                    {
                        Errors.Error_List.Add(
                            $"[Scanner Error] Unterminated string"
                        );
                    }

                    continue;
                }

                // COMMENT
                if (i + 1 < SourceCode.Length &&
                    SourceCode.Substring(i, 2) == "/*")
                {
                    i += 2;

                    while (i + 1 < SourceCode.Length &&
                           SourceCode.Substring(i, 2) != "*/")
                    {
                        i++;
                    }

                    if (i + 1 < SourceCode.Length)
                        i += 2;
                    else
                        Errors.Error_List.Add("[Scanner Error] Unterminated comment");

                    continue;
                }

                // TWO CHAR OPERATORS
                if (i + 1 < SourceCode.Length)
                {
                    string two = SourceCode.Substring(i, 2);

                    if (Operators.ContainsKey(two))
                    {
                        AddToken(two);
                        i += 2;
                        continue;
                    }
                }

                // SINGLE CHAR OPERATORS
                AddToken(CurrentChar.ToString());
                i++;
            }

            CompilerState.TokenStream = Tokens;
        }

        void AddToken(string lex)
        {
            if (Operators.ContainsKey(lex))
            {
                Tokens.Add(new Token
                {
                    lex = lex,
                    token_type = Operators[lex]
                });
            }
            else
            {
                FindTokenClass(lex);
            }
        }

        void FindTokenClass(string Lex)
        {
            Token t = new Token();
            t.lex = Lex;

            if (ReservedWords.ContainsKey(Lex.ToLower()))
            {
                t.token_type = ReservedWords[Lex.ToLower()];
                Tokens.Add(t);
            }
            else if (isIdentifier(Lex))
            {
                t.token_type = Token_Class.Identifier;
                Tokens.Add(t);
            }
            else if (isConstant(Lex))
            {
                t.token_type = Token_Class.Constant;
                Tokens.Add(t);
            }
            else
            {
                Errors.Error_List.Add(
                    $"[Scanner Error] Undefined token: {Lex}"
                );
            }
        }

        bool isIdentifier(string lex)
        {
            return Regex.IsMatch(lex, @"^[A-Za-z][A-Za-z0-9]*$");
        }

        bool isConstant(string lex)
        {
            return Regex.IsMatch(lex, @"^[0-9]+(\.[0-9]+)?$");
        }
    }
}