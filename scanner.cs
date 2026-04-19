using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TINY_Compiler
{
    public enum Token_Class
    {
        // Reserved words
        Int, Float, StringType,
        Read, Write,
        Repeat, Until,
        If, ElseIf, Else, Then, End,
        Return, Endl,

        // Operators & punctuation
        Semicolon, Comma,
        LParanthesis, RParanthesis,
        FunctionStartOp, FunctionEndOp,
        EqualOp, LessThanOp, GreaterThanOp, NotEqualOp,
        PlusOp, MinusOp, MultiplyOp, DivideOp,
        AndOp, OrOp,
        AssignmentOp,

        // Literals / names
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

            Operators["&&"] = Token_Class.AndOp;
            Operators["||"] = Token_Class.OrOp;
            Operators[":="] = Token_Class.AssignmentOp;
            Operators["<>"] = Token_Class.NotEqualOp;
            Operators[";"] = Token_Class.Semicolon;
            Operators[","] = Token_Class.Comma;
            Operators["("] = Token_Class.LParanthesis;
            Operators[")"] = Token_Class.RParanthesis;
            Operators["{"] = Token_Class.FunctionStartOp;
            Operators["}"] = Token_Class.FunctionEndOp;
            Operators["="] = Token_Class.EqualOp;
            Operators["<"] = Token_Class.LessThanOp;
            Operators[">"] = Token_Class.GreaterThanOp;
            Operators["+"] = Token_Class.PlusOp;
            Operators["-"] = Token_Class.MinusOp;
            Operators["*"] = Token_Class.MultiplyOp;
            Operators["/"] = Token_Class.DivideOp;
        }

        public void StartScanning(string SourceCode)
        {
            int i = 0;
            while (i < SourceCode.Length)
            {
                char CurrentChar = SourceCode[i];

                // Skip whitespace
                if (char.IsWhiteSpace(CurrentChar))
                {
                    i++;
                    continue;
                }

                string lexeme = "";

                // Identifier or reserved word
                if (char.IsLetter(CurrentChar))
                {
                    while (i < SourceCode.Length && char.IsLetterOrDigit(SourceCode[i]))
                        lexeme += SourceCode[i++];
                    FindTokenClass(lexeme);
                    continue;
                }
                // Number (integer or float)
                else if (char.IsDigit(CurrentChar))
                {
                    bool dotSeen = false;
                    while (i < SourceCode.Length &&
                          (char.IsDigit(SourceCode[i]) || (!dotSeen && SourceCode[i] == '.')))
                    {
                        if (SourceCode[i] == '.') dotSeen = true;
                        lexeme += SourceCode[i++];
                    }
                    // Check if letters come after number → invalid identifier
                    if (i < SourceCode.Length && char.IsLetter(SourceCode[i]))
                    {
                        while (i < SourceCode.Length && char.IsLetterOrDigit(SourceCode[i]))
                        {
                            lexeme += SourceCode[i++];
                        }

                        Tokens.Add(new Token
                        {
                            lex = lexeme,
                            token_type = Token_Class.Undefined
                        });

                        Errors.Error_List.Add($"[Scanner Error] Invalid identifier '{lexeme}': identifiers cannot start with a digit");
                    }
                    else
                    {
                        FindTokenClass(lexeme);
                    }
                    continue;
                }
                else
                {
                    // String literal  →  " anything "
                    if (CurrentChar == '"')
                    {
                        string strLex = "\"";
                        i++; // skip opening "
                        while (i < SourceCode.Length && SourceCode[i] != '"')
                            strLex += SourceCode[i++];
                        if (i < SourceCode.Length)
                        {
                            strLex += '"';
                            i++; // skip closing "
                            Tokens.Add(new Token { lex = strLex, token_type = Token_Class.StringLiteral });
                        }
                        else
                        {
                            // Unterminated string
                            Tokens.Add(new Token { lex = strLex, token_type = Token_Class.Undefined });
                            Errors.Error_List.Add($"[Scanner Error] Unterminated string: {strLex}");
                        }
                        continue;
                    }

                    // Comment  /* … */  → skip entirely
                    if (i + 1 < SourceCode.Length && SourceCode.Substring(i, 2) == "/*")
                    {
                        int start = i;
                        i += 2;
                        while (i + 1 < SourceCode.Length && SourceCode.Substring(i, 2) != "*/")
                            i++;
                        if (i + 1 < SourceCode.Length)
                        {
                            i += 2; // skip */
                        }
                        else
                        {
                            // Unterminated comment
                            Errors.Error_List.Add($"[Scanner Error] Unterminated comment starting at position {start}");
                            break;
                        }
                        continue;
                    }

                    // Two-character operators  (:=  <>  &&  ||)
                    if (i + 1 < SourceCode.Length)
                    {
                        string two = SourceCode.Substring(i, 2);
                        if (Operators.ContainsKey(two))
                        {
                            FindTokenClass(two);
                            i += 2;
                            continue;
                        }
                    }

                    // Single-character operator
                    FindTokenClass(CurrentChar.ToString());
                    i++;
                }
            }

            CompilerState.TokenStream = Tokens;
        }

        void FindTokenClass(string Lex)
        {
            Token Tok = new Token();
            Tok.lex = Lex;

            if (ReservedWords.ContainsKey(Lex.ToLower()))
            {
                Tok.token_type = ReservedWords[Lex.ToLower()];
            }
            else if (Operators.ContainsKey(Lex))
            {
                Tok.token_type = Operators[Lex];
            }
            else if (isIdentifier(Lex))
            {
                Tok.token_type = Token_Class.Identifier;
            }
            else if (isConstant(Lex))
            {
                Tok.token_type = Token_Class.Constant;
            }
            else
            {
                Tok.token_type = Token_Class.Undefined;
                Errors.Error_List.Add($"[Scanner Error] Undefined token: {Lex}");
            }

            Tokens.Add(Tok);
        }

        bool isIdentifier(string lex)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(lex, @"^[A-Za-z][A-Za-z0-9]*$");
        }

        bool isConstant(string lex)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(lex, @"^[0-9]+(\.[0-9]+)?$");
        }
    }
}