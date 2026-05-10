namespace TINY_Compiler
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            // ── Declare controls ─────────────────────────────────
            txtSource = new System.Windows.Forms.TextBox();
            btnCompile = new System.Windows.Forms.Button();
            btnClear = new System.Windows.Forms.Button();
            dataGridViewTokens = new System.Windows.Forms.DataGridView();
            colTokLex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colTokType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewSymbols = new System.Windows.Forms.DataGridView();
            colSymName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSymDType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            treeView1 = new System.Windows.Forms.TreeView();
            listBoxErrors = new System.Windows.Forms.ListBox();
            lblSource = new System.Windows.Forms.Label();
            lblTokens = new System.Windows.Forms.Label();
            lblSymbols = new System.Windows.Forms.Label();
            lblTree = new System.Windows.Forms.Label();
            lblErrors = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)dataGridViewTokens).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewSymbols).BeginInit();
            SuspendLayout();

            // ── Shared values ────────────────────────────────────
            var boldFont = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            var monoFont = new System.Drawing.Font("Consolas", 10F);
            var monoSmall = new System.Drawing.Font("Consolas", 9F);

            int col1X = 12; int col1W = 370;   // Source code
            int col2X = 394; int col2W = 290;   // Tokens (top) + Symbols (bottom)
            int col3X = 696; int col3W = 340;   // Parse tree
            int topY = 32;
            int formH = 690;

            // Token grid height + Symbol grid height + gap between them
            int tokH = 280;
            int symH = 220;
            int midGap = 14;
            int symY = topY + tokH + midGap + 22; // 22 = label height

            // ════ SOURCE CODE ════════════════════════════════════
            lblSource.Text = "Source Code:";
            lblSource.Font = boldFont;
            lblSource.AutoSize = true;
            lblSource.Location = new System.Drawing.Point(col1X, 10);

            txtSource.Location = new System.Drawing.Point(col1X, topY);
            txtSource.Multiline = true;
            txtSource.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtSource.Font = monoFont;
            txtSource.Size = new System.Drawing.Size(col1W, tokH + midGap + 22 + symH); // match right col height
            txtSource.TabIndex = 0;

            btnCompile.Text = "▶  Compile";
            btnCompile.Font = boldFont;
            btnCompile.Location = new System.Drawing.Point(col1X, topY + txtSource.Height + 8);
            btnCompile.Size = new System.Drawing.Size(155, 40);
            btnCompile.TabIndex = 1;
            btnCompile.Click += btnCompile_Click;

            btnClear.Text = "✕  Clear";
            btnClear.Font = boldFont;
            btnClear.Location = new System.Drawing.Point(col1X + 165, topY + txtSource.Height + 8);
            btnClear.Size = new System.Drawing.Size(120, 40);
            btnClear.TabIndex = 2;
            btnClear.Click += btnClear_Click;

            // ════ TOKEN STREAM (top-right of col2) ═══════════════
            lblTokens.Text = "Token Stream:";
            lblTokens.Font = boldFont;
            lblTokens.AutoSize = true;
            lblTokens.Location = new System.Drawing.Point(col2X, 10);

            dataGridViewTokens.AllowUserToAddRows = false;
            dataGridViewTokens.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewTokens.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colTokLex, colTokType });
            dataGridViewTokens.Location = new System.Drawing.Point(col2X, topY);
            dataGridViewTokens.Name = "dataGridViewTokens";
            dataGridViewTokens.RowHeadersWidth = 30;
            dataGridViewTokens.Size = new System.Drawing.Size(col2W, tokH);
            dataGridViewTokens.TabIndex = 3;

            colTokLex.HeaderText = "Lexeme";
            colTokLex.Name = "colTokLex";
            colTokLex.Width = 130;

            colTokType.HeaderText = "Token Type";
            colTokType.Name = "colTokType";
            colTokType.Width = 140;

            // ════ SYMBOL TABLE (bottom of col2) ══════════════════
            lblSymbols.Text = "Symbol Table:";
            lblSymbols.Font = boldFont;
            lblSymbols.AutoSize = true;
            lblSymbols.Location = new System.Drawing.Point(col2X, topY + tokH + midGap);

            dataGridViewSymbols.AllowUserToAddRows = false;
            dataGridViewSymbols.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewSymbols.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colSymName, colSymDType });
            dataGridViewSymbols.Location = new System.Drawing.Point(col2X, symY);
            dataGridViewSymbols.Name = "dataGridViewSymbols";
            dataGridViewSymbols.RowHeadersWidth = 30;
            dataGridViewSymbols.Size = new System.Drawing.Size(col2W, symH);
            dataGridViewSymbols.TabIndex = 4;

            colSymName.HeaderText = "Identifier";
            colSymName.Name = "colSymName";
            colSymName.Width = 130;

            colSymDType.HeaderText = "Declared As";
            colSymDType.Name = "colSymDType";
            colSymDType.Width = 140;

            // ════ PARSE TREE ══════════════════════════════════════
            lblTree.Text = "Parse Tree:";
            lblTree.Font = boldFont;
            lblTree.AutoSize = true;
            lblTree.Location = new System.Drawing.Point(col3X, 10);

            treeView1.Location = new System.Drawing.Point(col3X, topY);
            treeView1.Name = "treeView1";
            treeView1.Font = monoSmall;
            treeView1.Size = new System.Drawing.Size(col3W, tokH + midGap + 22 + symH);
            treeView1.TabIndex = 5;

            // ════ ERROR LIST (full-width bottom bar) ══════════════
            int errY = topY + txtSource.Height + 8 + 40 + 10;

            lblErrors.Text = "Errors / Warnings:";
            lblErrors.Font = boldFont;
            lblErrors.AutoSize = true;
            lblErrors.Location = new System.Drawing.Point(col1X, errY);

            listBoxErrors.Location = new System.Drawing.Point(col1X, errY + 22);
            listBoxErrors.Name = "listBoxErrors";
            listBoxErrors.Font = monoSmall;
            listBoxErrors.Size = new System.Drawing.Size(col1W + col2W + col3W + 20, 90);
            listBoxErrors.HorizontalScrollbar = true;
            listBoxErrors.TabIndex = 6;

            // ════ FORM ═══════════════════════════════════════════
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(col1X + col1W + col2W + col3W + 36,
                                                          errY + 22 + 90 + 12);
            Text = "TINY Compiler";
            MinimumSize = new System.Drawing.Size(800, 600);

            Controls.Add(lblSource);
            Controls.Add(txtSource);
            Controls.Add(btnCompile);
            Controls.Add(btnClear);
            Controls.Add(lblTokens);
            Controls.Add(dataGridViewTokens);
            Controls.Add(lblSymbols);
            Controls.Add(dataGridViewSymbols);
            Controls.Add(lblTree);
            Controls.Add(treeView1);
            Controls.Add(lblErrors);
            Controls.Add(listBoxErrors);

            ((System.ComponentModel.ISupportInitialize)dataGridViewTokens).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewSymbols).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        // ── Fields ────────────────────────────────────────────────
        private System.Windows.Forms.TextBox txtSource;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.DataGridView dataGridViewTokens;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTokLex;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTokType;
        private System.Windows.Forms.DataGridView dataGridViewSymbols;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSymName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSymDType;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ListBox listBoxErrors;
        private System.Windows.Forms.Label lblSource;
        private System.Windows.Forms.Label lblTokens;
        private System.Windows.Forms.Label lblSymbols;
        private System.Windows.Forms.Label lblTree;
        private System.Windows.Forms.Label lblErrors;
    }
}