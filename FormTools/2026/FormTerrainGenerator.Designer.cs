namespace FormTools
{
    partial class FormTerrainGenerator
    {
        private System.ComponentModel.IContainer components = null;
        private OpenGL.GlControl glControl1;
        private System.Windows.Forms.TextBox txt_command;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.glControl1 = new OpenGL.GlControl();
            this.txt_command = new System.Windows.Forms.TextBox();
            this.txtHistory = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // glControl1
            // 
            this.glControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.glControl1.BackColor = System.Drawing.Color.Black;
            this.glControl1.ColorBits = ((uint)(24u));
            this.glControl1.DepthBits = ((uint)(24u));
            this.glControl1.Location = new System.Drawing.Point(12, 12);
            this.glControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.glControl1.MultisampleBits = ((uint)(0u));
            this.glControl1.Name = "glControl1";
            this.glControl1.Size = new System.Drawing.Size(512, 512);
            this.glControl1.StencilBits = ((uint)(0u));
            this.glControl1.TabIndex = 0;
            this.glControl1.Render += new System.EventHandler<OpenGL.GlControlEventArgs>(this.glControl1_Render);
            // 
            // txt_command
            // 
            this.txt_command.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txt_command.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_command.Location = new System.Drawing.Point(12, 530);
            this.txt_command.Name = "txt_command";
            this.txt_command.Size = new System.Drawing.Size(1024, 26);
            this.txt_command.TabIndex = 1;
            this.txt_command.Text = "명령어 입력 (help 입력 시 도움말)";
            this.txt_command.Enter += new System.EventHandler(this.txt_command_Enter);
            this.txt_command.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txt_command_KeyDown);
            this.txt_command.Leave += new System.EventHandler(this.txt_command_Leave);
            // 
            // txtHistory
            // 
            this.txtHistory.Location = new System.Drawing.Point(531, 12);
            this.txtHistory.Multiline = true;
            this.txtHistory.Name = "txtHistory";
            this.txtHistory.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtHistory.Size = new System.Drawing.Size(505, 512);
            this.txtHistory.TabIndex = 2;
            // 
            // FormTerrainGenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1048, 611);
            this.Controls.Add(this.txtHistory);
            this.Controls.Add(this.txt_command);
            this.Controls.Add(this.glControl1);
            this.Name = "FormTerrainGenerator";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Terrain Generator";
            this.Load += new System.EventHandler(this.FormTerrainGenerator_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.TextBox txtHistory;
    }
}