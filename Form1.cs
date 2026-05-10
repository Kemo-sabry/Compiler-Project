using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TINY_Compiler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // ── Compile ───────────────────────────────────────────────
        private void btnCompile_Click(object sender, EventArgs e)
        {
            // Clear everything
            dataGridViewTokens.Rows.Clear();
            dataGridViewSymbols.Rows.Clear();
            listBoxErrors.Items.Clear();
            treeView1.Nodes.Clear();

            string code = txtSource.Text;

            if (string.IsNullOrWhiteSpace(code))
            {
                listBoxErrors.Items.Add("⚠  No source code to compile.");
                return;
            }

            // ── PHASE 1: Scanning ─────────────────────────────────
            TINY_Compiler.Start_Compiling(code);
            PopulateTokenGrid();

            // STOP if scanner found any lexical errors
            if (Errors.Error_List.Count > 0)
            {
                listBoxErrors.Items.Add("✖  Lexical errors found. Compilation stopped.");
                listBoxErrors.Items.Add("─────────────────────────────────────────");
               
                foreach (var err in Errors.Error_List)
                    listBoxErrors.Items.Add(err);
                return;
            }

            // ── PHASE 2: Parsing ──────────────────────────────────
            var parser = new Parser();
            Node parseRoot = parser.StartParsing(TINY_Compiler.TokenStream);

            // STOP if parser found any syntax errors
            if (Errors.Error_List.Count > 0)
            {
                listBoxErrors.Items.Add("✖  Syntax errors found. Compilation stopped.");
                listBoxErrors.Items.Add("─────────────────────────────────────────");
                foreach (var err in Errors.Error_List)
                    listBoxErrors.Items.Add(err);

                // Still show whatever partial tree was built
                TreeNode tvRoot = Parser.ToTreeView(parseRoot);
                if (tvRoot != null)
                {
                    treeView1.Nodes.Add(tvRoot);
                    treeView1.ExpandAll();
                }
                return;
            }

            // ── SUCCESS ───────────────────────────────────────────
            PopulateSymbolTable();

            TreeNode successRoot = Parser.ToTreeView(parseRoot);
            if (successRoot != null)
            {
                treeView1.Nodes.Add(successRoot);
                treeView1.ExpandAll();
            }

            listBoxErrors.Items.Add("✔  Compilation successful — no errors found.");
        }

        // ── Clear ─────────────────────────────────────────────────
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSource.Clear();
            dataGridViewTokens.Rows.Clear();
            dataGridViewSymbols.Rows.Clear();
            listBoxErrors.Items.Clear();
            treeView1.Nodes.Clear();
            TINY_Compiler.TokenStream.Clear();
            TINY_Compiler.tiny_Scanner.Tokens.Clear();
            Errors.Error_List.Clear();
        }

        // ── Helpers ───────────────────────────────────────────────
        void PopulateTokenGrid()
        {
            foreach (var tok in TINY_Compiler.tiny_Scanner.Tokens)
                dataGridViewTokens.Rows.Add(tok.lex, tok.token_type.ToString());
        }

        void PopulateSymbolTable()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tokens = TINY_Compiler.tiny_Scanner.Tokens;

            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];
                if (tok.token_type == Token_Class.Identifier && seen.Add(tok.lex))
                {
                    string dtype = "—";
                    if (i > 0)
                    {
                        var prev = tokens[i - 1];
                        if (prev.token_type == Token_Class.Int) dtype = "int";
                        else if (prev.token_type == Token_Class.Float) dtype = "float";
                        else if (prev.token_type == Token_Class.StringType) dtype = "string";
                    }
                    dataGridViewSymbols.Rows.Add(tok.lex, dtype);
                }
            }
        }
    }
}