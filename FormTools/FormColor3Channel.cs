using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FormTools
{
    public partial class FormColor3Channel : Form
    {
        public FormColor3Channel()
        {
            InitializeComponent();
        }

        private void FormColor3Channel_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fn = this.openFileDialog1.FileName;
                Bitmap bitmap = (Bitmap)Bitmap.FromFile(fn);
                this.pictureBox1.Image = bitmap;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.pictureBox2.Image = GetChannel((Bitmap)this.pictureBox1.Image, 0);
        }

        public Bitmap GetChannel(Bitmap src, int channel)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height);
            for (int i = 0; i < src.Height; i++)
            {
                for (int j = 0; j < src.Width; j++)
                {
                    Color c = src.GetPixel(j, i);
                    if (channel == 0) c = Color.FromArgb(c.R, 0, 0);
                    if (channel == 1) c = Color.FromArgb(0, c.G, 0);
                    if (channel == 2) c = Color.FromArgb(0, 0, c.B);
                    dst.SetPixel(j, i, c);
                }
            }
            return dst;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.pictureBox2.Image = GetChannel((Bitmap)this.pictureBox1.Image, 1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.pictureBox2.Image = GetChannel((Bitmap)this.pictureBox1.Image, 2);
        }
    }
}
