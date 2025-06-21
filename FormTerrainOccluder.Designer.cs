namespace OpenEng3d
{
    partial class FormTerrainOccluder
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
            this.components = new System.ComponentModel.Container();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.파일ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.열기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.테스트ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bSPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lINEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lINESTRIPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pOINTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.랜덤ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pOINTCROSSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.저장3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.cmdBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(16, 26);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(829, 576);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(27, 73);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(97, 30);
            this.button2.TabIndex = 5;
            this.button2.Text = "최적위치찾기";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click_1);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(27, 223);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(81, 30);
            this.button3.TabIndex = 6;
            this.button3.Text = "보기";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(27, 37);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(81, 30);
            this.button4.TabIndex = 7;
            this.button4.Text = "랜덤뿌리기";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(27, 145);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(81, 30);
            this.button6.TabIndex = 9;
            this.button6.Text = "보기";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 300;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.파일ToolStripMenuItem,
            this.테스트ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(855, 24);
            this.menuStrip1.TabIndex = 10;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 파일ToolStripMenuItem
            // 
            this.파일ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.열기ToolStripMenuItem,
            this.저장3ToolStripMenuItem});
            this.파일ToolStripMenuItem.Name = "파일ToolStripMenuItem";
            this.파일ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.파일ToolStripMenuItem.Text = "파일";
            // 
            // 열기ToolStripMenuItem
            // 
            this.열기ToolStripMenuItem.Name = "열기ToolStripMenuItem";
            this.열기ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.열기ToolStripMenuItem.Text = "열기";
            this.열기ToolStripMenuItem.Click += new System.EventHandler(this.열기ToolStripMenuItem_Click);
            // 
            // 테스트ToolStripMenuItem
            // 
            this.테스트ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bSPToolStripMenuItem,
            this.lINEToolStripMenuItem,
            this.lINESTRIPToolStripMenuItem,
            this.pOINTToolStripMenuItem,
            this.랜덤ToolStripMenuItem,
            this.pOINTCROSSToolStripMenuItem});
            this.테스트ToolStripMenuItem.Name = "테스트ToolStripMenuItem";
            this.테스트ToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
            this.테스트ToolStripMenuItem.Text = "테스트";
            // 
            // bSPToolStripMenuItem
            // 
            this.bSPToolStripMenuItem.Name = "bSPToolStripMenuItem";
            this.bSPToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.bSPToolStripMenuItem.Text = "BSP";
            this.bSPToolStripMenuItem.Click += new System.EventHandler(this.bSPToolStripMenuItem_Click);
            // 
            // lINEToolStripMenuItem
            // 
            this.lINEToolStripMenuItem.Name = "lINEToolStripMenuItem";
            this.lINEToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.lINEToolStripMenuItem.Text = "LINE";
            this.lINEToolStripMenuItem.Click += new System.EventHandler(this.lINEToolStripMenuItem_Click);
            // 
            // lINESTRIPToolStripMenuItem
            // 
            this.lINESTRIPToolStripMenuItem.Name = "lINESTRIPToolStripMenuItem";
            this.lINESTRIPToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.lINESTRIPToolStripMenuItem.Text = "LINE_STRIP";
            this.lINESTRIPToolStripMenuItem.Click += new System.EventHandler(this.lINESTRIPToolStripMenuItem_Click);
            // 
            // pOINTToolStripMenuItem
            // 
            this.pOINTToolStripMenuItem.Name = "pOINTToolStripMenuItem";
            this.pOINTToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.pOINTToolStripMenuItem.Text = "POINT";
            this.pOINTToolStripMenuItem.Click += new System.EventHandler(this.pOINTToolStripMenuItem_Click);
            // 
            // 랜덤ToolStripMenuItem
            // 
            this.랜덤ToolStripMenuItem.Name = "랜덤ToolStripMenuItem";
            this.랜덤ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.랜덤ToolStripMenuItem.Text = "랜덤";
            this.랜덤ToolStripMenuItem.Click += new System.EventHandler(this.랜덤ToolStripMenuItem_Click);
            // 
            // pOINTCROSSToolStripMenuItem
            // 
            this.pOINTCROSSToolStripMenuItem.Name = "pOINTCROSSToolStripMenuItem";
            this.pOINTCROSSToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.pOINTCROSSToolStripMenuItem.Text = "POINT_CROSS";
            this.pOINTCROSSToolStripMenuItem.Click += new System.EventHandler(this.pOINTCROSSToolStripMenuItem_Click);
            // 
            // 저장3ToolStripMenuItem
            // 
            this.저장3ToolStripMenuItem.Name = "저장3ToolStripMenuItem";
            this.저장3ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.저장3ToolStripMenuItem.Text = "저장3";
            this.저장3ToolStripMenuItem.Click += new System.EventHandler(this.저장3ToolStripMenuItem_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // cmdBox
            // 
            this.cmdBox.Location = new System.Drawing.Point(114, 37);
            this.cmdBox.Name = "cmdBox";
            this.cmdBox.Size = new System.Drawing.Size(175, 21);
            this.cmdBox.TabIndex = 11;
            this.cmdBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cmdBox_KeyDown);
            // 
            // FormTerrainOccluder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(855, 631);
            this.Controls.Add(this.cmdBox);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormTerrainOccluder";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormTerrainOccluder";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.FormTerrainOccluder_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 파일ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 열기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 테스트ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bSPToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lINEToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lINESTRIPToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pOINTToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 랜덤ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pOINTCROSSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 저장3ToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox cmdBox;
    }
}