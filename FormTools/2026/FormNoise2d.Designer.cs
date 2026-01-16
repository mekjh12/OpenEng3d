namespace FormTools
{
    partial class FormNoise2d
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
            this.glControl1 = new OpenGL.GlControl();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.파일ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.저장ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.종료ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.chk_color = new System.Windows.Forms.CheckBox();
            this.sld_lacunarity = new FormTools.LabeledSlider();
            this.sld_persistence = new FormTools.LabeledSlider();
            this.sld_octaves = new FormTools.LabeledSlider();
            this.sld_scale = new FormTools.LabeledSlider();
            this.cmb_mode = new System.Windows.Forms.ComboBox();
            this.worley_cellsize = new FormTools.LabeledSlider();
            this.cmb_worley = new System.Windows.Forms.ComboBox();
            this.worley_lacunarity = new FormTools.LabeledSlider();
            this.worley_octaves = new FormTools.LabeledSlider();
            this.worley_gain = new FormTools.LabeledSlider();
            this.chkFlip = new System.Windows.Forms.CheckBox();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sld_lacunarity.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_persistence.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_octaves.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_scale.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.worley_cellsize.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.worley_lacunarity.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.worley_octaves.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.worley_gain.TrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // glControl1
            // 
            this.glControl1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.glControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.glControl1.ColorBits = ((uint)(24u));
            this.glControl1.DepthBits = ((uint)(24u));
            this.glControl1.Location = new System.Drawing.Point(13, 27);
            this.glControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.glControl1.MultisampleBits = ((uint)(0u));
            this.glControl1.Name = "glControl1";
            this.glControl1.Size = new System.Drawing.Size(948, 876);
            this.glControl1.StencilBits = ((uint)(8u));
            this.glControl1.TabIndex = 3;
            this.glControl1.Render += new System.EventHandler<OpenGL.GlControlEventArgs>(this.glControl1_Render);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.파일ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1385, 24);
            this.menuStrip1.TabIndex = 10;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 파일ToolStripMenuItem
            // 
            this.파일ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.저장ToolStripMenuItem,
            this.toolStripSeparator1,
            this.종료ToolStripMenuItem});
            this.파일ToolStripMenuItem.Name = "파일ToolStripMenuItem";
            this.파일ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.파일ToolStripMenuItem.Text = "파일";
            // 
            // 저장ToolStripMenuItem
            // 
            this.저장ToolStripMenuItem.Name = "저장ToolStripMenuItem";
            this.저장ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.저장ToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.저장ToolStripMenuItem.Text = "저장";
            this.저장ToolStripMenuItem.Click += new System.EventHandler(this.저장ToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(136, 6);
            // 
            // 종료ToolStripMenuItem
            // 
            this.종료ToolStripMenuItem.Name = "종료ToolStripMenuItem";
            this.종료ToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.종료ToolStripMenuItem.Text = "종료";
            this.종료ToolStripMenuItem.Click += new System.EventHandler(this.종료ToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 919);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1385, 22);
            this.statusStrip1.TabIndex = 11;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(91, 17);
            this.toolStripStatusLabel1.Text = "상태정보표시창";
            // 
            // chk_color
            // 
            this.chk_color.AutoSize = true;
            this.chk_color.Location = new System.Drawing.Point(968, 38);
            this.chk_color.Name = "chk_color";
            this.chk_color.Size = new System.Drawing.Size(72, 16);
            this.chk_color.TabIndex = 12;
            this.chk_color.Text = "컬러모드";
            this.chk_color.UseVisualStyleBackColor = true;
            this.chk_color.CheckedChanged += new System.EventHandler(this.chk_color_CheckedChanged);
            // 
            // sld_lacunarity
            // 
            this.sld_lacunarity.BackColor = System.Drawing.SystemColors.Control;
            this.sld_lacunarity.Location = new System.Drawing.Point(968, 168);
            this.sld_lacunarity.Maximum = 500;
            this.sld_lacunarity.Minimum = 100;
            this.sld_lacunarity.Name = "sld_lacunarity";
            this.sld_lacunarity.Size = new System.Drawing.Size(400, 35);
            this.sld_lacunarity.TabIndex = 8;
            this.sld_lacunarity.Title = "Lacunarity";
            // 
            // 
            // 
            this.sld_lacunarity.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.sld_lacunarity.TrackBar.Maximum = 500;
            this.sld_lacunarity.TrackBar.Minimum = 100;
            this.sld_lacunarity.TrackBar.Name = "";
            this.sld_lacunarity.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.sld_lacunarity.TrackBar.TabIndex = 1;
            this.sld_lacunarity.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_lacunarity.TrackBar.Value = 120;
            this.sld_lacunarity.Value = 120;
            this.sld_lacunarity.ValueChanged += new System.EventHandler(this.sld_lacunarity_ValueChanged);
            // 
            // sld_persistence
            // 
            this.sld_persistence.BackColor = System.Drawing.SystemColors.Control;
            this.sld_persistence.Location = new System.Drawing.Point(968, 139);
            this.sld_persistence.Maximum = 100;
            this.sld_persistence.Minimum = 10;
            this.sld_persistence.Name = "sld_persistence";
            this.sld_persistence.Size = new System.Drawing.Size(400, 35);
            this.sld_persistence.TabIndex = 7;
            this.sld_persistence.Title = "Persistence";
            // 
            // 
            // 
            this.sld_persistence.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.sld_persistence.TrackBar.Maximum = 100;
            this.sld_persistence.TrackBar.Minimum = 10;
            this.sld_persistence.TrackBar.Name = "";
            this.sld_persistence.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.sld_persistence.TrackBar.TabIndex = 1;
            this.sld_persistence.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_persistence.TrackBar.Value = 50;
            this.sld_persistence.Value = 50;
            this.sld_persistence.ValueChanged += new System.EventHandler(this.sld_persistence_ValueChanged);
            // 
            // sld_octaves
            // 
            this.sld_octaves.BackColor = System.Drawing.SystemColors.Control;
            this.sld_octaves.Location = new System.Drawing.Point(968, 80);
            this.sld_octaves.Minimum = 1;
            this.sld_octaves.Name = "sld_octaves";
            this.sld_octaves.Size = new System.Drawing.Size(400, 35);
            this.sld_octaves.TabIndex = 6;
            this.sld_octaves.Title = "Octaves";
            // 
            // 
            // 
            this.sld_octaves.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.sld_octaves.TrackBar.Minimum = 1;
            this.sld_octaves.TrackBar.Name = "";
            this.sld_octaves.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.sld_octaves.TrackBar.TabIndex = 1;
            this.sld_octaves.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_octaves.TrackBar.Value = 5;
            this.sld_octaves.Value = 5;
            this.sld_octaves.ValueChanged += new System.EventHandler(this.sld_octaves_ValueChanged);
            // 
            // sld_scale
            // 
            this.sld_scale.BackColor = System.Drawing.SystemColors.Control;
            this.sld_scale.Location = new System.Drawing.Point(968, 108);
            this.sld_scale.Maximum = 100;
            this.sld_scale.Name = "sld_scale";
            this.sld_scale.Size = new System.Drawing.Size(400, 35);
            this.sld_scale.TabIndex = 5;
            this.sld_scale.Title = "Scale";
            // 
            // 
            // 
            this.sld_scale.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.sld_scale.TrackBar.Maximum = 100;
            this.sld_scale.TrackBar.Name = "";
            this.sld_scale.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.sld_scale.TrackBar.TabIndex = 1;
            this.sld_scale.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_scale.TrackBar.Value = 50;
            this.sld_scale.Value = 50;
            this.sld_scale.ValueChanged += new System.EventHandler(this.sld_scale_ValueChanged);
            // 
            // cmb_mode
            // 
            this.cmb_mode.FormattingEnabled = true;
            this.cmb_mode.Items.AddRange(new object[] {
            "Perlin Noise - 부드러운 그라디언트 노이즈",
            "Simplex Noise - Perlin보다 빠르고 자연스러움"});
            this.cmb_mode.Location = new System.Drawing.Point(968, 60);
            this.cmb_mode.Name = "cmb_mode";
            this.cmb_mode.Size = new System.Drawing.Size(291, 20);
            this.cmb_mode.TabIndex = 13;
            this.cmb_mode.Text = "Perlin Noise - 부드러운 그라디언트 노이즈";
            this.cmb_mode.SelectedIndexChanged += new System.EventHandler(this.cmb_mode_SelectedIndexChanged);
            // 
            // worley_cellsize
            // 
            this.worley_cellsize.BackColor = System.Drawing.SystemColors.Control;
            this.worley_cellsize.Location = new System.Drawing.Point(968, 270);
            this.worley_cellsize.Maximum = 1000;
            this.worley_cellsize.Minimum = 100;
            this.worley_cellsize.Name = "worley_cellsize";
            this.worley_cellsize.Size = new System.Drawing.Size(400, 35);
            this.worley_cellsize.TabIndex = 14;
            this.worley_cellsize.Title = "Cellsize";
            // 
            // 
            // 
            this.worley_cellsize.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.worley_cellsize.TrackBar.Maximum = 1000;
            this.worley_cellsize.TrackBar.Minimum = 100;
            this.worley_cellsize.TrackBar.Name = "";
            this.worley_cellsize.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.worley_cellsize.TrackBar.TabIndex = 1;
            this.worley_cellsize.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.worley_cellsize.TrackBar.Value = 100;
            this.worley_cellsize.Value = 100;
            this.worley_cellsize.ValueChanged += new System.EventHandler(this.worely_lacunarity_ValueChanged);
            this.worley_cellsize.Load += new System.EventHandler(this.worely_lacunarity_Load);
            // 
            // cmb_worley
            // 
            this.cmb_worley.FormattingEnabled = true;
            this.cmb_worley.Items.AddRange(new object[] {
            "Perlin FBM: 큰 형태 (베이스)",
            "Worley Noise: 구름의 뭉게뭉게한 디테일",
            "Worley FBM: 다층 디테일"});
            this.cmb_worley.Location = new System.Drawing.Point(968, 218);
            this.cmb_worley.Name = "cmb_worley";
            this.cmb_worley.Size = new System.Drawing.Size(291, 20);
            this.cmb_worley.TabIndex = 15;
            this.cmb_worley.Text = "Perlin Noise - 부드러운 그라디언트 노이즈";
            this.cmb_worley.SelectedIndexChanged += new System.EventHandler(this.cmb_worley_SelectedIndexChanged);
            // 
            // worley_lacunarity
            // 
            this.worley_lacunarity.BackColor = System.Drawing.SystemColors.Control;
            this.worley_lacunarity.Location = new System.Drawing.Point(968, 298);
            this.worley_lacunarity.Maximum = 500;
            this.worley_lacunarity.Minimum = 10;
            this.worley_lacunarity.Name = "worley_lacunarity";
            this.worley_lacunarity.Size = new System.Drawing.Size(400, 35);
            this.worley_lacunarity.TabIndex = 16;
            this.worley_lacunarity.Title = "Lacunarity";
            // 
            // 
            // 
            this.worley_lacunarity.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.worley_lacunarity.TrackBar.Maximum = 500;
            this.worley_lacunarity.TrackBar.Minimum = 10;
            this.worley_lacunarity.TrackBar.Name = "";
            this.worley_lacunarity.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.worley_lacunarity.TrackBar.TabIndex = 1;
            this.worley_lacunarity.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.worley_lacunarity.TrackBar.Value = 200;
            this.worley_lacunarity.Value = 200;
            this.worley_lacunarity.ValueChanged += new System.EventHandler(this.worley_lacunarity_ValueChanged);
            // 
            // worley_octaves
            // 
            this.worley_octaves.BackColor = System.Drawing.SystemColors.Control;
            this.worley_octaves.Location = new System.Drawing.Point(968, 244);
            this.worley_octaves.Minimum = 1;
            this.worley_octaves.Name = "worley_octaves";
            this.worley_octaves.Size = new System.Drawing.Size(400, 35);
            this.worley_octaves.TabIndex = 17;
            this.worley_octaves.Title = "Octaves";
            // 
            // 
            // 
            this.worley_octaves.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.worley_octaves.TrackBar.Minimum = 1;
            this.worley_octaves.TrackBar.Name = "";
            this.worley_octaves.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.worley_octaves.TrackBar.TabIndex = 1;
            this.worley_octaves.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.worley_octaves.TrackBar.Value = 5;
            this.worley_octaves.Value = 5;
            this.worley_octaves.ValueChanged += new System.EventHandler(this.worley_octaves_ValueChanged);
            // 
            // worley_gain
            // 
            this.worley_gain.BackColor = System.Drawing.SystemColors.Control;
            this.worley_gain.Location = new System.Drawing.Point(968, 329);
            this.worley_gain.Maximum = 500;
            this.worley_gain.Minimum = 10;
            this.worley_gain.Name = "worley_gain";
            this.worley_gain.Size = new System.Drawing.Size(400, 35);
            this.worley_gain.TabIndex = 18;
            this.worley_gain.Title = "Gain";
            // 
            // 
            // 
            this.worley_gain.TrackBar.Location = new System.Drawing.Point(85, 5);
            this.worley_gain.TrackBar.Maximum = 500;
            this.worley_gain.TrackBar.Minimum = 10;
            this.worley_gain.TrackBar.Name = "";
            this.worley_gain.TrackBar.Size = new System.Drawing.Size(200, 45);
            this.worley_gain.TrackBar.TabIndex = 1;
            this.worley_gain.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.worley_gain.TrackBar.Value = 20;
            this.worley_gain.Value = 20;
            // 
            // chkFlip
            // 
            this.chkFlip.AutoSize = true;
            this.chkFlip.Location = new System.Drawing.Point(1046, 38);
            this.chkFlip.Name = "chkFlip";
            this.chkFlip.Size = new System.Drawing.Size(72, 16);
            this.chkFlip.TabIndex = 19;
            this.chkFlip.Text = "반전모드";
            this.chkFlip.UseVisualStyleBackColor = true;
            this.chkFlip.CheckedChanged += new System.EventHandler(this.chkFlip_CheckedChanged);
            // 
            // FormNoise2d
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1385, 941);
            this.Controls.Add(this.chkFlip);
            this.Controls.Add(this.worley_gain);
            this.Controls.Add(this.worley_octaves);
            this.Controls.Add(this.worley_lacunarity);
            this.Controls.Add(this.cmb_worley);
            this.Controls.Add(this.worley_cellsize);
            this.Controls.Add(this.cmb_mode);
            this.Controls.Add(this.chk_color);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.sld_lacunarity);
            this.Controls.Add(this.sld_persistence);
            this.Controls.Add(this.sld_octaves);
            this.Controls.Add(this.sld_scale);
            this.Controls.Add(this.glControl1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "FormNoise2d";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormNoise2d";
            this.Load += new System.EventHandler(this.FormNoise2d_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sld_lacunarity.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_persistence.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_octaves.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_scale.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.worley_cellsize.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.worley_lacunarity.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.worley_octaves.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.worley_gain.TrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private OpenGL.GlControl glControl1;
        private LabeledSlider sld_scale;
        private LabeledSlider sld_octaves;
        private LabeledSlider sld_persistence;
        private LabeledSlider sld_lacunarity;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 파일ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 저장ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem 종료ToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.CheckBox chk_color;
        private System.Windows.Forms.ComboBox cmb_mode;
        private LabeledSlider worley_cellsize;
        private System.Windows.Forms.ComboBox cmb_worley;
        private LabeledSlider worley_lacunarity;
        private LabeledSlider worley_octaves;
        private LabeledSlider worley_gain;
        private System.Windows.Forms.CheckBox chkFlip;
    }
}