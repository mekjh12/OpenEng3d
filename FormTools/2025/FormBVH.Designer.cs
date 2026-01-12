namespace FormTools
{
    partial class FormBVH
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
            this.SuspendLayout();
            // 
            // FormBVH
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1130, 683);
            this.Location = new System.Drawing.Point(100, 100);
            this.Name = "FormBVH";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "FormBVH";
            this.Load += new System.EventHandler(this.FormBVH_Load);
            this.ResumeLayout(false);

        }

        #endregion
    }
}