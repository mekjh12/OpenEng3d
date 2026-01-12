namespace FormTools
{
    partial class FormTileBaker
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.파일ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.종료ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.도구ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.terrainRegionBakeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lowFileConvertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simpleMapConvertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.리전타일부드럽게하기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.증폭하기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.리전타일침식적용ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 27);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(309, 425);
            this.textBox1.TabIndex = 5;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(327, 27);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox2.Size = new System.Drawing.Size(479, 425);
            this.textBox2.TabIndex = 6;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.파일ToolStripMenuItem,
            this.도구ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(818, 24);
            this.menuStrip1.TabIndex = 9;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 파일ToolStripMenuItem
            // 
            this.파일ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.종료ToolStripMenuItem});
            this.파일ToolStripMenuItem.Name = "파일ToolStripMenuItem";
            this.파일ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.파일ToolStripMenuItem.Text = "파일";
            // 
            // 종료ToolStripMenuItem
            // 
            this.종료ToolStripMenuItem.Name = "종료ToolStripMenuItem";
            this.종료ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.종료ToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            this.종료ToolStripMenuItem.Text = "종료";
            this.종료ToolStripMenuItem.Click += new System.EventHandler(this.종료ToolStripMenuItem_Click);
            // 
            // 도구ToolStripMenuItem
            // 
            this.도구ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.terrainRegionBakeToolStripMenuItem,
            this.lowFileConvertToolStripMenuItem,
            this.simpleMapConvertToolStripMenuItem,
            this.리전타일부드럽게하기ToolStripMenuItem,
            this.증폭하기ToolStripMenuItem,
            this.리전타일침식적용ToolStripMenuItem});
            this.도구ToolStripMenuItem.Name = "도구ToolStripMenuItem";
            this.도구ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.도구ToolStripMenuItem.Text = "도구";
            // 
            // terrainRegionBakeToolStripMenuItem
            // 
            this.terrainRegionBakeToolStripMenuItem.Name = "terrainRegionBakeToolStripMenuItem";
            this.terrainRegionBakeToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.terrainRegionBakeToolStripMenuItem.Text = "TerrainRegion Bake";
            this.terrainRegionBakeToolStripMenuItem.Click += new System.EventHandler(this.terrainRegionBakeToolStripMenuItem_Click);
            // 
            // lowFileConvertToolStripMenuItem
            // 
            this.lowFileConvertToolStripMenuItem.Name = "lowFileConvertToolStripMenuItem";
            this.lowFileConvertToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.lowFileConvertToolStripMenuItem.Text = "LowFile 합치기";
            this.lowFileConvertToolStripMenuItem.Click += new System.EventHandler(this.lowFileConvertToolStripMenuItem_Click);
            // 
            // simpleMapConvertToolStripMenuItem
            // 
            this.simpleMapConvertToolStripMenuItem.Name = "simpleMapConvertToolStripMenuItem";
            this.simpleMapConvertToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.simpleMapConvertToolStripMenuItem.Text = "SimpleMap Convert";
            // 
            // 리전타일부드럽게하기ToolStripMenuItem
            // 
            this.리전타일부드럽게하기ToolStripMenuItem.Name = "리전타일부드럽게하기ToolStripMenuItem";
            this.리전타일부드럽게하기ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.리전타일부드럽게하기ToolStripMenuItem.Text = "리전타일 부드럽게 하기";
            this.리전타일부드럽게하기ToolStripMenuItem.Click += new System.EventHandler(this.리전타일부드럽게하기ToolStripMenuItem_Click);
            // 
            // 증폭하기ToolStripMenuItem
            // 
            this.증폭하기ToolStripMenuItem.Name = "증폭하기ToolStripMenuItem";
            this.증폭하기ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.증폭하기ToolStripMenuItem.Text = "증폭하기";
            this.증폭하기ToolStripMenuItem.Click += new System.EventHandler(this.증폭하기ToolStripMenuItem_Click);
            // 
            // 리전타일침식적용ToolStripMenuItem
            // 
            this.리전타일침식적용ToolStripMenuItem.Name = "리전타일침식적용ToolStripMenuItem";
            this.리전타일침식적용ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.리전타일침식적용ToolStripMenuItem.Text = "리전타일 침식 적용";
            this.리전타일침식적용ToolStripMenuItem.Click += new System.EventHandler(this.리전타일침식적용ToolStripMenuItem_Click);
            // 
            // FormTileBaker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(818, 463);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormTileBaker";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormTileBaker";
            this.Load += new System.EventHandler(this.FormTileBaker_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 파일ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 도구ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lowFileConvertToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem simpleMapConvertToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem terrainRegionBakeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 종료ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 리전타일부드럽게하기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 증폭하기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 리전타일침식적용ToolStripMenuItem;
    }
}