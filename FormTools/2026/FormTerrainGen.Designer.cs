namespace FormTools
{
    partial class FormTerrainGen
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
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.파일ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.새지형ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.종료ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.도구ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pNGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.읽어오기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.저장하기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.지형보기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.지형이미지타일링하기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.지형이미지업스케일ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.지형이미지다듬기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.도움말ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.txtCoord = new System.Windows.Forms.TextBox();
            this.btn_refresh = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.button2 = new System.Windows.Forms.Button();
            this.btnLoad = new System.Windows.Forms.Button();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.label1 = new System.Windows.Forms.Label();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.pic_river = new FormTools.LabeledPictureBox();
            this.pic_normal = new FormTools.LabeledPictureBox();
            this.pic_heightmap = new FormTools.LabeledPictureBox();
            this.sld_octaves = new FormTools.LabeledSlider();
            this.sld_scale = new FormTools.LabeledSlider();
            this.txtConsole = new System.Windows.Forms.TextBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_river)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_normal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_heightmap)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_octaves.TrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_scale.TrackBar)).BeginInit();
            this.SuspendLayout();
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
            this.glControl1.Location = new System.Drawing.Point(4, 27);
            this.glControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.glControl1.MultisampleBits = ((uint)(0u));
            this.glControl1.Name = "glControl1";
            this.glControl1.Size = new System.Drawing.Size(800, 800);
            this.glControl1.StencilBits = ((uint)(0u));
            this.glControl1.TabIndex = 11;
            this.glControl1.Render += new System.EventHandler<OpenGL.GlControlEventArgs>(this.glControl1_Render);
            this.glControl1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.glControl1_KeyUp);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 886);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1616, 22);
            this.statusStrip1.TabIndex = 15;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(121, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Click += new System.EventHandler(this.toolStripStatusLabel1_Click);
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(300, 16);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.파일ToolStripMenuItem,
            this.도구ToolStripMenuItem,
            this.도움말ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1616, 24);
            this.menuStrip1.TabIndex = 16;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 파일ToolStripMenuItem
            // 
            this.파일ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.새지형ToolStripMenuItem,
            this.toolStripSeparator1,
            this.종료ToolStripMenuItem});
            this.파일ToolStripMenuItem.Name = "파일ToolStripMenuItem";
            this.파일ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.파일ToolStripMenuItem.Text = "파일";
            // 
            // 새지형ToolStripMenuItem
            // 
            this.새지형ToolStripMenuItem.Name = "새지형ToolStripMenuItem";
            this.새지형ToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.새지형ToolStripMenuItem.Text = "새 지형";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(111, 6);
            // 
            // 종료ToolStripMenuItem
            // 
            this.종료ToolStripMenuItem.Name = "종료ToolStripMenuItem";
            this.종료ToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.종료ToolStripMenuItem.Text = "종료";
            // 
            // 도구ToolStripMenuItem
            // 
            this.도구ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pNGToolStripMenuItem,
            this.지형보기ToolStripMenuItem,
            this.지형이미지타일링하기ToolStripMenuItem,
            this.지형이미지업스케일ToolStripMenuItem,
            this.지형이미지다듬기ToolStripMenuItem});
            this.도구ToolStripMenuItem.Name = "도구ToolStripMenuItem";
            this.도구ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.도구ToolStripMenuItem.Text = "도구";
            // 
            // pNGToolStripMenuItem
            // 
            this.pNGToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.읽어오기ToolStripMenuItem,
            this.저장하기ToolStripMenuItem});
            this.pNGToolStripMenuItem.Name = "pNGToolStripMenuItem";
            this.pNGToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.pNGToolStripMenuItem.Text = "PNG";
            // 
            // 읽어오기ToolStripMenuItem
            // 
            this.읽어오기ToolStripMenuItem.Name = "읽어오기ToolStripMenuItem";
            this.읽어오기ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.읽어오기ToolStripMenuItem.Text = "읽어오기";
            this.읽어오기ToolStripMenuItem.Click += new System.EventHandler(this.읽어오기ToolStripMenuItem_Click);
            // 
            // 저장하기ToolStripMenuItem
            // 
            this.저장하기ToolStripMenuItem.Name = "저장하기ToolStripMenuItem";
            this.저장하기ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.저장하기ToolStripMenuItem.Text = "저장하기";
            // 
            // 지형보기ToolStripMenuItem
            // 
            this.지형보기ToolStripMenuItem.Name = "지형보기ToolStripMenuItem";
            this.지형보기ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.지형보기ToolStripMenuItem.Text = "지형 보기";
            this.지형보기ToolStripMenuItem.Click += new System.EventHandler(this.지형보기ToolStripMenuItem_Click);
            // 
            // 지형이미지타일링하기ToolStripMenuItem
            // 
            this.지형이미지타일링하기ToolStripMenuItem.Name = "지형이미지타일링하기ToolStripMenuItem";
            this.지형이미지타일링하기ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.지형이미지타일링하기ToolStripMenuItem.Text = "지형이미지 타일링 하기";
            this.지형이미지타일링하기ToolStripMenuItem.Click += new System.EventHandler(this.지형이미지타일링하기ToolStripMenuItem_Click);
            // 
            // 지형이미지업스케일ToolStripMenuItem
            // 
            this.지형이미지업스케일ToolStripMenuItem.Name = "지형이미지업스케일ToolStripMenuItem";
            this.지형이미지업스케일ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.지형이미지업스케일ToolStripMenuItem.Text = "지형이미지 업스케일";
            this.지형이미지업스케일ToolStripMenuItem.Click += new System.EventHandler(this.지형이미지업스케일ToolStripMenuItem_Click);
            // 
            // 지형이미지다듬기ToolStripMenuItem
            // 
            this.지형이미지다듬기ToolStripMenuItem.Name = "지형이미지다듬기ToolStripMenuItem";
            this.지형이미지다듬기ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.지형이미지다듬기ToolStripMenuItem.Text = "지형이미지 다듬기";
            this.지형이미지다듬기ToolStripMenuItem.Click += new System.EventHandler(this.지형이미지다듬기ToolStripMenuItem_Click);
            // 
            // 도움말ToolStripMenuItem
            // 
            this.도움말ToolStripMenuItem.Name = "도움말ToolStripMenuItem";
            this.도움말ToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
            this.도움말ToolStripMenuItem.Text = "도움말";
            // 
            // txtCoord
            // 
            this.txtCoord.Location = new System.Drawing.Point(1520, 142);
            this.txtCoord.Name = "txtCoord";
            this.txtCoord.Size = new System.Drawing.Size(74, 21);
            this.txtCoord.TabIndex = 20;
            this.txtCoord.Text = "0x0";
            // 
            // btn_refresh
            // 
            this.btn_refresh.Location = new System.Drawing.Point(1503, 382);
            this.btn_refresh.Name = "btn_refresh";
            this.btn_refresh.Size = new System.Drawing.Size(74, 29);
            this.btn_refresh.TabIndex = 21;
            this.btn_refresh.Text = "새로고침";
            this.btn_refresh.UseVisualStyleBackColor = true;
            this.btn_refresh.Click += new System.EventHandler(this.btn_refresh_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1445, 408);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(149, 64);
            this.button2.TabIndex = 25;
            this.button2.Text = "지형Base가져오기";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(1445, 184);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(113, 30);
            this.btnLoad.TabIndex = 26;
            this.btnLoad.Text = "이미지타일 읽기";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // fileSystemWatcher1
            // 
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.SynchronizingObject = this;
            this.fileSystemWatcher1.Changed += new System.IO.FileSystemEventHandler(this.fileSystemWatcher1_Changed);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1520, 166);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 12);
            this.label1.TabIndex = 28;
            this.label1.Text = "label1";
            // 
            // pic_river
            // 
            this.pic_river.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pic_river.LabelBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.pic_river.LabelFont = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.pic_river.LabelForeColor = System.Drawing.Color.White;
            this.pic_river.LabelPadding = new System.Windows.Forms.Padding(5, 2, 5, 2);
            this.pic_river.LabelText = "River";
            this.pic_river.Location = new System.Drawing.Point(1494, 258);
            this.pic_river.Name = "pic_river";
            this.pic_river.Size = new System.Drawing.Size(100, 100);
            this.pic_river.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pic_river.TabIndex = 19;
            this.pic_river.TabStop = false;
            this.pic_river.Click += new System.EventHandler(this.pic_river_Click);
            // 
            // pic_normal
            // 
            this.pic_normal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pic_normal.LabelBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.pic_normal.LabelFont = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.pic_normal.LabelForeColor = System.Drawing.Color.White;
            this.pic_normal.LabelPadding = new System.Windows.Forms.Padding(5, 2, 5, 2);
            this.pic_normal.LabelText = "Normal";
            this.pic_normal.Location = new System.Drawing.Point(1445, 220);
            this.pic_normal.Name = "pic_normal";
            this.pic_normal.Size = new System.Drawing.Size(100, 100);
            this.pic_normal.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pic_normal.TabIndex = 18;
            this.pic_normal.TabStop = false;
            this.pic_normal.Click += new System.EventHandler(this.pic_normal_Click);
            this.pic_normal.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pic_normal_MouseDoubleClick);
            // 
            // pic_heightmap
            // 
            this.pic_heightmap.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pic_heightmap.LabelBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.pic_heightmap.LabelFont = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.pic_heightmap.LabelForeColor = System.Drawing.Color.White;
            this.pic_heightmap.LabelPadding = new System.Windows.Forms.Padding(5, 2, 5, 2);
            this.pic_heightmap.LabelText = "Height";
            this.pic_heightmap.Location = new System.Drawing.Point(1416, 276);
            this.pic_heightmap.Name = "pic_heightmap";
            this.pic_heightmap.Size = new System.Drawing.Size(100, 100);
            this.pic_heightmap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pic_heightmap.TabIndex = 17;
            this.pic_heightmap.TabStop = false;
            this.pic_heightmap.Click += new System.EventHandler(this.pic_heightmap_Click);
            // 
            // sld_octaves
            // 
            this.sld_octaves.BackColor = System.Drawing.SystemColors.Control;
            this.sld_octaves.LargeChange = 1;
            this.sld_octaves.Location = new System.Drawing.Point(1445, 478);
            this.sld_octaves.Minimum = 1;
            this.sld_octaves.Name = "sld_octaves";
            this.sld_octaves.Size = new System.Drawing.Size(144, 35);
            this.sld_octaves.TabIndex = 13;
            this.sld_octaves.Title = "Octaves";
            // 
            // 
            // 
            this.sld_octaves.TrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sld_octaves.TrackBar.AutoSize = false;
            this.sld_octaves.TrackBar.LargeChange = 1;
            this.sld_octaves.TrackBar.Location = new System.Drawing.Point(120, 10);
            this.sld_octaves.TrackBar.Minimum = 1;
            this.sld_octaves.TrackBar.Name = "";
            this.sld_octaves.TrackBar.Size = new System.Drawing.Size(14, 30);
            this.sld_octaves.TrackBar.TabIndex = 1;
            this.sld_octaves.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_octaves.TrackBar.Value = 5;
            this.sld_octaves.Value = 5;
            this.sld_octaves.ValueChanged += new System.EventHandler(this.sld_octaves_ValueChanged);
            // 
            // sld_scale
            // 
            this.sld_scale.BackColor = System.Drawing.SystemColors.Control;
            this.sld_scale.LargeChange = 1;
            this.sld_scale.Location = new System.Drawing.Point(1445, 519);
            this.sld_scale.Maximum = 5000;
            this.sld_scale.Minimum = 1;
            this.sld_scale.Name = "sld_scale";
            this.sld_scale.Size = new System.Drawing.Size(144, 35);
            this.sld_scale.TabIndex = 12;
            this.sld_scale.Title = "Scale";
            // 
            // 
            // 
            this.sld_scale.TrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sld_scale.TrackBar.AutoSize = false;
            this.sld_scale.TrackBar.LargeChange = 1;
            this.sld_scale.TrackBar.Location = new System.Drawing.Point(120, 10);
            this.sld_scale.TrackBar.Maximum = 5000;
            this.sld_scale.TrackBar.Minimum = 1;
            this.sld_scale.TrackBar.Name = "";
            this.sld_scale.TrackBar.Size = new System.Drawing.Size(14, 30);
            this.sld_scale.TrackBar.TabIndex = 1;
            this.sld_scale.TrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sld_scale.TrackBar.Value = 5;
            this.sld_scale.Value = 5;
            this.sld_scale.ValueChanged += new System.EventHandler(this.sld_scale_ValueChanged);
            // 
            // txtConsole
            // 
            this.txtConsole.Location = new System.Drawing.Point(811, 27);
            this.txtConsole.Multiline = true;
            this.txtConsole.Name = "txtConsole";
            this.txtConsole.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtConsole.Size = new System.Drawing.Size(449, 800);
            this.txtConsole.TabIndex = 29;
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(811, 833);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(74, 29);
            this.btnClear.TabIndex = 30;
            this.btnClear.Text = "지우기";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(891, 833);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(369, 29);
            this.button1.TabIndex = 31;
            this.button1.Text = "지형 타일링 후 raw(high+low+riverRoad) 생성하기";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FormTerrainGen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1616, 908);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.txtConsole);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.btn_refresh);
            this.Controls.Add(this.txtCoord);
            this.Controls.Add(this.pic_river);
            this.Controls.Add(this.pic_normal);
            this.Controls.Add(this.pic_heightmap);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.sld_octaves);
            this.Controls.Add(this.sld_scale);
            this.Controls.Add(this.glControl1);
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormTerrainGen";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "지형생성기";
            this.Load += new System.EventHandler(this.FormTerrainGen_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_river)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_normal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_heightmap)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_octaves.TrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sld_scale.TrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private OpenGL.GlControl glControl1;
        private LabeledSlider sld_scale;
        private LabeledSlider sld_octaves;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 파일ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 종료ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 도움말ToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private LabeledPictureBox pic_heightmap;
        private LabeledPictureBox pic_normal;
        private LabeledPictureBox pic_river;
        private System.Windows.Forms.ToolStripMenuItem 새지형ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.TextBox txtCoord;
        private System.Windows.Forms.Button btn_refresh;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem 도구ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pNGToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 읽어오기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 저장하기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 지형보기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 지형이미지타일링하기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 지형이미지다듬기ToolStripMenuItem;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnLoad;
        public System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem 지형이미지업스케일ToolStripMenuItem;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private System.Windows.Forms.TextBox txtConsole;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button button1;
    }
}