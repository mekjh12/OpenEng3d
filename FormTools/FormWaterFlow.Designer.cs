namespace FormTools
{
    partial class FormWaterFlow
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
            this.picOriginal = new System.Windows.Forms.PictureBox();
            this.picResult = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.지형처리ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.강줄기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.읽어오기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.결과저장하기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.처리하기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.물흐름처리하기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.glControl1 = new OpenGL.GlControl();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.txtPrint = new System.Windows.Forms.TextBox();
            this.picCompose = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picOriginal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picResult)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCompose)).BeginInit();
            this.SuspendLayout();
            // 
            // picOriginal
            // 
            this.picOriginal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picOriginal.Location = new System.Drawing.Point(12, 27);
            this.picOriginal.Name = "picOriginal";
            this.picOriginal.Size = new System.Drawing.Size(400, 400);
            this.picOriginal.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOriginal.TabIndex = 0;
            this.picOriginal.TabStop = false;
            // 
            // picResult
            // 
            this.picResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picResult.Location = new System.Drawing.Point(418, 27);
            this.picResult.Name = "picResult";
            this.picResult.Size = new System.Drawing.Size(400, 400);
            this.picResult.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picResult.TabIndex = 1;
            this.picResult.TabStop = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.지형처리ToolStripMenuItem,
            this.강줄기ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1846, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 지형처리ToolStripMenuItem
            // 
            this.지형처리ToolStripMenuItem.Name = "지형처리ToolStripMenuItem";
            this.지형처리ToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
            this.지형처리ToolStripMenuItem.Text = "지형처리";
            // 
            // 강줄기ToolStripMenuItem
            // 
            this.강줄기ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.읽어오기ToolStripMenuItem,
            this.결과저장하기ToolStripMenuItem,
            this.처리하기ToolStripMenuItem,
            this.물흐름처리하기ToolStripMenuItem});
            this.강줄기ToolStripMenuItem.Name = "강줄기ToolStripMenuItem";
            this.강줄기ToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
            this.강줄기ToolStripMenuItem.Text = "강줄기";
            // 
            // 읽어오기ToolStripMenuItem
            // 
            this.읽어오기ToolStripMenuItem.Name = "읽어오기ToolStripMenuItem";
            this.읽어오기ToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.읽어오기ToolStripMenuItem.Text = "[그림1] 읽어오기";
            this.읽어오기ToolStripMenuItem.Click += new System.EventHandler(this.읽어오기ToolStripMenuItem_Click);
            // 
            // 결과저장하기ToolStripMenuItem
            // 
            this.결과저장하기ToolStripMenuItem.Name = "결과저장하기ToolStripMenuItem";
            this.결과저장하기ToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.결과저장하기ToolStripMenuItem.Text = "[그림2] 결과 저장하기";
            this.결과저장하기ToolStripMenuItem.Click += new System.EventHandler(this.결과저장하기ToolStripMenuItem_Click);
            // 
            // 처리하기ToolStripMenuItem
            // 
            this.처리하기ToolStripMenuItem.Name = "처리하기ToolStripMenuItem";
            this.처리하기ToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.처리하기ToolStripMenuItem.Text = "가우시안블러 처리하기";
            // 
            // 물흐름처리하기ToolStripMenuItem
            // 
            this.물흐름처리하기ToolStripMenuItem.Name = "물흐름처리하기ToolStripMenuItem";
            this.물흐름처리하기ToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.물흐름처리하기ToolStripMenuItem.Text = "물흐름 처리하기";
            this.물흐름처리하기ToolStripMenuItem.Click += new System.EventHandler(this.물흐름처리하기ToolStripMenuItem_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // glControl1
            // 
            this.glControl1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.glControl1.ColorBits = ((uint)(24u));
            this.glControl1.DepthBits = ((uint)(0u));
            this.glControl1.Location = new System.Drawing.Point(13, 800);
            this.glControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.glControl1.MultisampleBits = ((uint)(0u));
            this.glControl1.Name = "glControl1";
            this.glControl1.Size = new System.Drawing.Size(399, 122);
            this.glControl1.StencilBits = ((uint)(0u));
            this.glControl1.TabIndex = 3;
            // 
            // txtPrint
            // 
            this.txtPrint.Location = new System.Drawing.Point(418, 433);
            this.txtPrint.Multiline = true;
            this.txtPrint.Name = "txtPrint";
            this.txtPrint.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPrint.Size = new System.Drawing.Size(400, 492);
            this.txtPrint.TabIndex = 4;
            // 
            // picCompose
            // 
            this.picCompose.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picCompose.Location = new System.Drawing.Point(824, 27);
            this.picCompose.Name = "picCompose";
            this.picCompose.Size = new System.Drawing.Size(1010, 895);
            this.picCompose.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picCompose.TabIndex = 8;
            this.picCompose.TabStop = false;
            // 
            // FormWaterFlow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1846, 937);
            this.Controls.Add(this.picCompose);
            this.Controls.Add(this.txtPrint);
            this.Controls.Add(this.glControl1);
            this.Controls.Add(this.picResult);
            this.Controls.Add(this.picOriginal);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormWaterFlow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormWaterFlow";
            this.Load += new System.EventHandler(this.FormWaterFlow_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picOriginal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picResult)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCompose)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picOriginal;
        private System.Windows.Forms.PictureBox picResult;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 지형처리ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 강줄기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 읽어오기ToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem 처리하기ToolStripMenuItem;
        private OpenGL.GlControl glControl1;
        private System.Windows.Forms.ToolStripMenuItem 결과저장하기ToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem 물흐름처리하기ToolStripMenuItem;
        private System.Windows.Forms.TextBox txtPrint;
        private System.Windows.Forms.PictureBox picCompose;
    }
}