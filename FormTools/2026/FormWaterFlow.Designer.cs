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
            this.picReadBuffer = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.팡리ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.열기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.종료ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.실행ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.초기화ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.실행ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.중앙에물뿌리기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.테스트ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.랜덤하게물뿌리기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.보기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gPU에서가져오기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.txtPrint = new System.Windows.Forms.TextBox();
            this.picWriteBuffer = new System.Windows.Forms.PictureBox();
            this.glControl1 = new OpenGL.GlControl();
            this.sld_color_scaled = new FormTools.LabeledSlider();
            this.sld_velocity = new FormTools.LabeledSlider();
            this.chk_useHeightMap = new System.Windows.Forms.CheckBox();
            this.chk_auto_rain = new System.Windows.Forms.CheckBox();
            this.chk_play = new System.Windows.Forms.CheckBox();
            this.sld_rain_amout = new FormTools.LabeledSlider();
            this.sld_evaporationFactor = new FormTools.LabeledSlider();
            this.btnExport = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picOriginal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picReadBuffer)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picWriteBuffer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_color_scaled.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_velocity.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_rain_amout.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_evaporationFactor.TrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // picOriginal
            // 
            this.picOriginal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picOriginal.Location = new System.Drawing.Point(1319, 29);
            this.picOriginal.Name = "picOriginal";
            this.picOriginal.Size = new System.Drawing.Size(300, 300);
            this.picOriginal.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOriginal.TabIndex = 0;
            this.picOriginal.TabStop = false;
            // 
            // picReadBuffer
            // 
            this.picReadBuffer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picReadBuffer.Location = new System.Drawing.Point(1319, 335);
            this.picReadBuffer.Name = "picReadBuffer";
            this.picReadBuffer.Size = new System.Drawing.Size(300, 300);
            this.picReadBuffer.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picReadBuffer.TabIndex = 1;
            this.picReadBuffer.TabStop = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.팡리ToolStripMenuItem,
            this.실행ToolStripMenuItem,
            this.보기ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1635, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 팡리ToolStripMenuItem
            // 
            this.팡리ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.열기ToolStripMenuItem,
            this.toolStripSeparator1,
            this.종료ToolStripMenuItem});
            this.팡리ToolStripMenuItem.Name = "팡리ToolStripMenuItem";
            this.팡리ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.팡리ToolStripMenuItem.Text = "파일";
            // 
            // 열기ToolStripMenuItem
            // 
            this.열기ToolStripMenuItem.Name = "열기ToolStripMenuItem";
            this.열기ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.열기ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.열기ToolStripMenuItem.Text = "열기";
            this.열기ToolStripMenuItem.Click += new System.EventHandler(this.열기ToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // 종료ToolStripMenuItem
            // 
            this.종료ToolStripMenuItem.Name = "종료ToolStripMenuItem";
            this.종료ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.종료ToolStripMenuItem.Text = "종료";
            this.종료ToolStripMenuItem.Click += new System.EventHandler(this.종료ToolStripMenuItem_Click);
            // 
            // 실행ToolStripMenuItem
            // 
            this.실행ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.초기화ToolStripMenuItem,
            this.실행ToolStripMenuItem1,
            this.중앙에물뿌리기ToolStripMenuItem,
            this.테스트ToolStripMenuItem,
            this.랜덤하게물뿌리기ToolStripMenuItem});
            this.실행ToolStripMenuItem.Name = "실행ToolStripMenuItem";
            this.실행ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.실행ToolStripMenuItem.Text = "실행";
            // 
            // 초기화ToolStripMenuItem
            // 
            this.초기화ToolStripMenuItem.Name = "초기화ToolStripMenuItem";
            this.초기화ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Q)));
            this.초기화ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.초기화ToolStripMenuItem.Text = "초기화";
            this.초기화ToolStripMenuItem.Click += new System.EventHandler(this.초기화ToolStripMenuItem_Click);
            // 
            // 실행ToolStripMenuItem1
            // 
            this.실행ToolStripMenuItem1.Name = "실행ToolStripMenuItem1";
            this.실행ToolStripMenuItem1.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.실행ToolStripMenuItem1.Size = new System.Drawing.Size(211, 22);
            this.실행ToolStripMenuItem1.Text = "실행";
            this.실행ToolStripMenuItem1.Click += new System.EventHandler(this.실행ToolStripMenuItem1_Click);
            // 
            // 중앙에물뿌리기ToolStripMenuItem
            // 
            this.중앙에물뿌리기ToolStripMenuItem.Name = "중앙에물뿌리기ToolStripMenuItem";
            this.중앙에물뿌리기ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
            this.중앙에물뿌리기ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.중앙에물뿌리기ToolStripMenuItem.Text = "중앙에 물 뿌리기";
            this.중앙에물뿌리기ToolStripMenuItem.Click += new System.EventHandler(this.중앙에물뿌리기ToolStripMenuItem_Click);
            // 
            // 테스트ToolStripMenuItem
            // 
            this.테스트ToolStripMenuItem.Name = "테스트ToolStripMenuItem";
            this.테스트ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.테스트ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.테스트ToolStripMenuItem.Text = "테스트";
            this.테스트ToolStripMenuItem.Click += new System.EventHandler(this.테스트ToolStripMenuItem_Click);
            // 
            // 랜덤하게물뿌리기ToolStripMenuItem
            // 
            this.랜덤하게물뿌리기ToolStripMenuItem.Name = "랜덤하게물뿌리기ToolStripMenuItem";
            this.랜덤하게물뿌리기ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.랜덤하게물뿌리기ToolStripMenuItem.Text = "랜덤하게 물 뿌리기";
            // 
            // 보기ToolStripMenuItem
            // 
            this.보기ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gPU에서가져오기ToolStripMenuItem});
            this.보기ToolStripMenuItem.Name = "보기ToolStripMenuItem";
            this.보기ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.보기ToolStripMenuItem.Text = "보기";
            // 
            // gPU에서가져오기ToolStripMenuItem
            // 
            this.gPU에서가져오기ToolStripMenuItem.Name = "gPU에서가져오기ToolStripMenuItem";
            this.gPU에서가져오기ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.gPU에서가져오기ToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.gPU에서가져오기ToolStripMenuItem.Text = "GPU에서 가져오기";
            this.gPU에서가져오기ToolStripMenuItem.Click += new System.EventHandler(this.gPU에서가져오기ToolStripMenuItem_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // txtPrint
            // 
            this.txtPrint.Location = new System.Drawing.Point(913, 231);
            this.txtPrint.Multiline = true;
            this.txtPrint.Name = "txtPrint";
            this.txtPrint.ReadOnly = true;
            this.txtPrint.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPrint.Size = new System.Drawing.Size(400, 709);
            this.txtPrint.TabIndex = 4;
            // 
            // picWriteBuffer
            // 
            this.picWriteBuffer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picWriteBuffer.Location = new System.Drawing.Point(1319, 641);
            this.picWriteBuffer.Name = "picWriteBuffer";
            this.picWriteBuffer.Size = new System.Drawing.Size(300, 300);
            this.picWriteBuffer.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picWriteBuffer.TabIndex = 8;
            this.picWriteBuffer.TabStop = false;
            // 
            // glControl1
            // 
            this.glControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.glControl1.Animation = true;
            this.glControl1.BackColor = System.Drawing.Color.Silver;
            this.glControl1.ColorBits = ((uint)(24u));
            this.glControl1.DepthBits = ((uint)(24u));
            this.glControl1.Location = new System.Drawing.Point(5, 29);
            this.glControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.glControl1.MultisampleBits = ((uint)(0u));
            this.glControl1.Name = "glControl1";
            this.glControl1.Size = new System.Drawing.Size(900, 911);
            this.glControl1.StencilBits = ((uint)(0u));
            this.glControl1.TabIndex = 10;
            this.glControl1.Render += new System.EventHandler<OpenGL.GlControlEventArgs>(this.glControl1_Render);
            this.glControl1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.glControl1_KeyUp);
            // 
            // sld_color_scaled
            // 
            this.sld_color_scaled.BackColor = System.Drawing.SystemColors.Control;
            this.sld_color_scaled.LargeChange = 1;
            this.sld_color_scaled.Location = new System.Drawing.Point(913, 127);
            this.sld_color_scaled.Maximum = 100;
            this.sld_color_scaled.Minimum = 1;
            this.sld_color_scaled.Name = "sld_color_scaled";
            this.sld_color_scaled.Size = new System.Drawing.Size(400, 35);
            this.sld_color_scaled.TabIndex = 11;
            this.sld_color_scaled.Title = "색상스케일";
            // 
            // 
            // 
            this.sld_color_scaled.TrackBar.LargeChange = 1;
            this.sld_color_scaled.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.sld_color_scaled.TrackBar.Maximum = 100;
            this.sld_color_scaled.TrackBar.Minimum = 1;
            this.sld_color_scaled.TrackBar.Name = "";
            this.sld_color_scaled.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.sld_color_scaled.TrackBar.TabIndex = 1;
            this.sld_color_scaled.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_color_scaled.TrackBar.Value = 1;
            this.sld_color_scaled.Value = 1;
            this.sld_color_scaled.DragLeave += new System.EventHandler(this.sld_color_scaled_DragLeave);
            // 
            // sld_velocity
            // 
            this.sld_velocity.BackColor = System.Drawing.SystemColors.Control;
            this.sld_velocity.LargeChange = 1;
            this.sld_velocity.Location = new System.Drawing.Point(913, 95);
            this.sld_velocity.Maximum = 1000;
            this.sld_velocity.Minimum = 1;
            this.sld_velocity.Name = "sld_velocity";
            this.sld_velocity.Size = new System.Drawing.Size(400, 35);
            this.sld_velocity.TabIndex = 12;
            this.sld_velocity.Title = "이동속도조절";
            // 
            // 
            // 
            this.sld_velocity.TrackBar.LargeChange = 1;
            this.sld_velocity.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.sld_velocity.TrackBar.Maximum = 1000;
            this.sld_velocity.TrackBar.Minimum = 1;
            this.sld_velocity.TrackBar.Name = "";
            this.sld_velocity.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.sld_velocity.TrackBar.TabIndex = 1;
            this.sld_velocity.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_velocity.TrackBar.Value = 1;
            this.sld_velocity.Value = 1;
            // 
            // chk_useHeightMap
            // 
            this.chk_useHeightMap.AutoSize = true;
            this.chk_useHeightMap.Checked = true;
            this.chk_useHeightMap.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_useHeightMap.Location = new System.Drawing.Point(913, 73);
            this.chk_useHeightMap.Name = "chk_useHeightMap";
            this.chk_useHeightMap.Size = new System.Drawing.Size(114, 16);
            this.chk_useHeightMap.TabIndex = 13;
            this.chk_useHeightMap.Text = "Use Height Map";
            this.chk_useHeightMap.UseVisualStyleBackColor = true;
            // 
            // chk_auto_rain
            // 
            this.chk_auto_rain.AutoSize = true;
            this.chk_auto_rain.Checked = true;
            this.chk_auto_rain.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_auto_rain.Location = new System.Drawing.Point(912, 51);
            this.chk_auto_rain.Name = "chk_auto_rain";
            this.chk_auto_rain.Size = new System.Drawing.Size(102, 16);
            this.chk_auto_rain.TabIndex = 14;
            this.chk_auto_rain.Text = "Auto Rainning";
            this.chk_auto_rain.UseVisualStyleBackColor = true;
            // 
            // chk_play
            // 
            this.chk_play.AutoSize = true;
            this.chk_play.Checked = true;
            this.chk_play.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_play.Location = new System.Drawing.Point(913, 29);
            this.chk_play.Name = "chk_play";
            this.chk_play.Size = new System.Drawing.Size(80, 16);
            this.chk_play.TabIndex = 15;
            this.chk_play.Text = "Play/Stop";
            this.chk_play.UseVisualStyleBackColor = true;
            // 
            // sld_rain_amout
            // 
            this.sld_rain_amout.BackColor = System.Drawing.SystemColors.Control;
            this.sld_rain_amout.LargeChange = 1;
            this.sld_rain_amout.Location = new System.Drawing.Point(914, 159);
            this.sld_rain_amout.Maximum = 200;
            this.sld_rain_amout.Minimum = 1;
            this.sld_rain_amout.Name = "sld_rain_amout";
            this.sld_rain_amout.Size = new System.Drawing.Size(400, 35);
            this.sld_rain_amout.TabIndex = 16;
            this.sld_rain_amout.Title = "비의 양";
            // 
            // 
            // 
            this.sld_rain_amout.TrackBar.LargeChange = 1;
            this.sld_rain_amout.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.sld_rain_amout.TrackBar.Maximum = 200;
            this.sld_rain_amout.TrackBar.Minimum = 1;
            this.sld_rain_amout.TrackBar.Name = "";
            this.sld_rain_amout.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.sld_rain_amout.TrackBar.TabIndex = 1;
            this.sld_rain_amout.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_rain_amout.TrackBar.Value = 100;
            this.sld_rain_amout.Value = 100;
            // 
            // sld_evaporationFactor
            // 
            this.sld_evaporationFactor.BackColor = System.Drawing.SystemColors.Control;
            this.sld_evaporationFactor.LargeChange = 1;
            this.sld_evaporationFactor.Location = new System.Drawing.Point(913, 190);
            this.sld_evaporationFactor.Maximum = 1000;
            this.sld_evaporationFactor.Minimum = 1;
            this.sld_evaporationFactor.Name = "sld_evaporationFactor";
            this.sld_evaporationFactor.Size = new System.Drawing.Size(400, 35);
            this.sld_evaporationFactor.TabIndex = 17;
            this.sld_evaporationFactor.Title = "증발률";
            // 
            // 
            // 
            this.sld_evaporationFactor.TrackBar.LargeChange = 1;
            this.sld_evaporationFactor.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.sld_evaporationFactor.TrackBar.Maximum = 1000;
            this.sld_evaporationFactor.TrackBar.Minimum = 1;
            this.sld_evaporationFactor.TrackBar.Name = "";
            this.sld_evaporationFactor.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.sld_evaporationFactor.TrackBar.TabIndex = 1;
            this.sld_evaporationFactor.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_evaporationFactor.TrackBar.Value = 1;
            this.sld_evaporationFactor.Value = 1;
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(1183, 29);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(130, 60);
            this.btnExport.TabIndex = 18;
            this.btnExport.Text = "내보내기(PNG)";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // FormWaterFlow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1635, 952);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.sld_evaporationFactor);
            this.Controls.Add(this.sld_rain_amout);
            this.Controls.Add(this.chk_play);
            this.Controls.Add(this.chk_auto_rain);
            this.Controls.Add(this.chk_useHeightMap);
            this.Controls.Add(this.sld_velocity);
            this.Controls.Add(this.sld_color_scaled);
            this.Controls.Add(this.glControl1);
            this.Controls.Add(this.picWriteBuffer);
            this.Controls.Add(this.txtPrint);
            this.Controls.Add(this.picReadBuffer);
            this.Controls.Add(this.picOriginal);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormWaterFlow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormWaterFlow";
            this.Load += new System.EventHandler(this.FormWaterFlow_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picOriginal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picReadBuffer)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picWriteBuffer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_color_scaled.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_velocity.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_rain_amout.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_evaporationFactor.TrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picOriginal;
        private System.Windows.Forms.PictureBox picReadBuffer;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.TextBox txtPrint;
        private System.Windows.Forms.PictureBox picWriteBuffer;
        private System.Windows.Forms.ToolStripMenuItem 팡리ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 종료ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 열기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem 실행ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 초기화ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 실행ToolStripMenuItem1;
        private OpenGL.GlControl glControl1;
        private System.Windows.Forms.ToolStripMenuItem 보기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gPU에서가져오기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 중앙에물뿌리기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 테스트ToolStripMenuItem;
        private LabeledSlider sld_color_scaled;
        private System.Windows.Forms.ToolStripMenuItem 랜덤하게물뿌리기ToolStripMenuItem;
        private LabeledSlider sld_velocity;
        private System.Windows.Forms.CheckBox chk_useHeightMap;
        private System.Windows.Forms.CheckBox chk_auto_rain;
        private System.Windows.Forms.CheckBox chk_play;
        private LabeledSlider sld_rain_amout;
        private LabeledSlider sld_evaporationFactor;
        private System.Windows.Forms.Button btnExport;
    }
}