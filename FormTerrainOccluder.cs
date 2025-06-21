using Occlusion;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Terrain;
using ZetaExt;
using BSP;
using Camera3d;

namespace OpenEng3d
{
    public partial class FormTerrainOccluder : Form
    {
        Bitmap bmp = null;
        Bitmap _org = null;
        TerrainMap _map = null;
        List<TerrainOccluder3> _list3 = new List<TerrainOccluder3>();

        enum MODE { LINE, LINE_STRIP, POINT, POINT_CROSS }

        MODE _mode = MODE.LINE;

        string filename = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\Res\209147.png";
        //BSP<TerrainOccluder> bsp = null;

        BspTree bsp = new BspTree();

        public FormTerrainOccluder()
        {
            InitializeComponent();
        }

        private void FormTerrainOccluder_Load(object sender, EventArgs e)
        {
            this.pictureBox1.Size = new System.Drawing.Size(this.Width, this.Height);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            Rand.InitSeed(0);

            _map = new TerrainMap();
            _map.LoadHeightMap(filename);

            bmp = (Bitmap)Bitmap.FromFile(filename);
            _org = bmp;
            this.pictureBox1.Image = bmp;

        }

        public void Draw(List<BspNode> list)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            Bitmap bmp1 = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(bmp1);
            int depth = 0;

            Font font = new Font(new FontFamily("휴먼명조"), 18.0f);
            foreach (BspNode node in list)
            {
                Vertex2f t = new Vertex2f(_map.Size, _map.Size);
                Vertex2f a = node.Segment.Start.xy() + t;
                Vertex2f b = node.Segment.End.xy() + t;
                Vertex2f c = (a + b) * 0.5f;
                Vertex3f color = Rand.NextColor3f * 255.0f;
                g.DrawLine(new Pen(Color.FromArgb((int)color.x, (int)color.y, (int)color.z), 5.0f), a.x, a.y, b.x, b.y);
                g.DrawString(node.Text, font, Brushes.Red, a.x, a.y);

                depth++;
            }
            g.Dispose();
            this.pictureBox1.Image = MergedBitmaps(bmp, bmp1);
        }

        public void Draw()
        {
            int w = bmp.Width;
            int h = bmp.Height;
            Bitmap bmp1 = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(bmp1);

            Font font = new Font(new FontFamily("휴먼명조"), 18.0f);
            foreach (TerrainOccluder3 occluder in _list3)
            {
                Vertex3f t = new Vertex3f(_map.Size, _map.Size, 0);
                Vertex3f a = occluder.Left + t;
                Vertex3f b = occluder.Right + t;
                Vertex3f center = (a + b) * 0.5f;
                Vertex3f c = Rand.NextColor3f * 255.0f;
                g.DrawLine(new Pen(Color.FromArgb((int)c.x, (int)c.y, (int)c.z), 5.0f), a.x, a.y, b.x, b.y);
                g.DrawString(occluder.ID +"", font, Brushes.Red, a.x, a.y);

            }
            g.Dispose();

            this.pictureBox1.Image = MergedBitmaps(bmp, bmp1);
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            int w = bmp.Width;
            int h = bmp.Height;

        }

        private Bitmap MergedBitmaps(Bitmap bmp1, Bitmap bmp2)
        {
            Graphics g = Graphics.FromImage(bmp1);
            g.DrawImage(bmp2, Point.Empty);
            g.Dispose();
            return bmp1;
        }


        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void MakeCrossCursor(float x, float y)
        {
        }

        private Vertex2f FindNext(Vertex2f fix, Vertex2f l, float d = 3.0f)
        {
            float max = _map.GetArea(l, fix);
            Vertex2f maxPos = l;
            float angle = 0;

            float dst = Rand.Next(d * 0.5f, d * 1.5f);

            for (int i = 0; i < 360; i++)
            {
                float theta = i;
                float dx = dst * (float)Math.Cos(theta.ToRadian());
                float dy = dst * (float)Math.Sin(theta.ToRadian());

                Vertex2f target = new Vertex2f(l.x + dx, l.y + dy);
                float area = _map.GetArea(fix, target);
                float dist = (fix - target).Norm();
                float height = area / dist;
                if (dist / height > 2.0f) continue;
                if (area > max)
                {
                    angle = i;
                    maxPos = target;
                    max = area;
                    break;
                }
            }

            return maxPos;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            

        }

        private void button5_Click(object sender, EventArgs e)
        {
            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            
        }


        Vertex2f p = Vertex2f.Zero;

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            int w = (int)(bmp.Width * 0.5f);
            int h = (int)(bmp.Height * 0.5f);

            float cx = ((float)e.X / (float)this.pictureBox1.Width) * bmp.Width;
            float cy = ((float)e.Y / (float)this.pictureBox1.Height) * bmp.Height;
            Vertex2f q = new Vertex2f(cx - w, cy - h);

            float minHeight = _map.GetMinimalHeight(p, q);

            Vertex3f a = _map.GetHeight(p.x, p.y);
            Vertex3f b = _map.GetHeight(q.x, q.y);

            (Vertex3f pa, Vertex3f pb) = _map.GetSlopeHeight(a, b);

            if (_mode == MODE.LINE)
            {
                Segment3 seg = new Segment3(pa, pb);
                bsp.Insert(seg);

                _list3.Add(new TerrainOccluder3(pa, pb));
                p = Vertex2f.Zero;
                this.Text = "LINE";
                Draw();
            }
            else if (_mode == MODE.POINT)
            {
                bmp = (Bitmap)Bitmap.FromFile(filename);
                _org = bmp;
                this.pictureBox1.Image = bmp;
                //Draw();
                Draw(bsp.GetConvexNodes(q));
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            int w = (int)(bmp.Width * 0.5f);
            int h = (int)(bmp.Height * 0.5f);

            float cx = ((float)e.X / (float)this.pictureBox1.Width) * bmp.Width;
            float cy = ((float)e.Y / (float)this.pictureBox1.Height) * bmp.Height;
            Vertex2f q = new Vertex2f(cx - w, cy - h);

            p = q;
        }

        private void bSPToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
        }

        private void lINEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mode = MODE.LINE;
        }

        private void lINESTRIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mode = MODE.LINE_STRIP;
        }

        private void pOINTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mode = MODE.POINT;
        }

        private void 랜덤ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rand.InitSeed(0);
            float range = 900.0f;
            for (int i = 0; i < 100; i++)
            {
                Vertex2f p = new Vertex2f(Rand.NextFloat2 * range, Rand.NextFloat2 * range);
                Vertex2f r = new Vertex2f(Rand.NextFloat2 * 35, Rand.NextFloat2 * 35);
                Vertex2f q = p + r;
                float minHeight = _map.GetMinimalHeight(p, q);
                //_list.Add(new TerrainOccluder(p, q, minHeight));
                //bsp.Insert(new Segment(p, q));
            }
            Draw();
        }

        private void pOINTCROSSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mode = MODE.POINT_CROSS;
        }

        private void 저장3ToolStripMenuItem_Click(object sender, EventArgs e)
        { 
            // save
            StreamWriter sw = new StreamWriter(@"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\Res\209147.occ3");
            foreach (TerrainOccluder3 occluder in _list3)
            {
                sw.WriteLine($"{occluder.Left.x},{occluder.Left.y},{occluder.Left.z},{occluder.Right.x},{occluder.Right.y},{occluder.Right.z}");
            }
            sw.Close();
            this.Text = "저장완료!";

        }

        private void 열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.InitialDirectory = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\Res\";
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = this.openFileDialog1.FileName;
                ReadOccluder(filename);
            }
        }

        public void ReadOccluder(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new Exception($"{filename}이 없습니다.");
            }

            StreamReader sr = new StreamReader(filename);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line.StartsWith("//")) continue;
                string[] cols = line.Split(',');
                if (cols.Length != 6) continue;
                Vertex3f left = new Vertex3f(float.Parse(cols[0].Trim()), float.Parse(cols[1].Trim()), float.Parse(cols[2].Trim()));
                Vertex3f right = new Vertex3f(float.Parse(cols[3].Trim()), float.Parse(cols[4].Trim()), float.Parse(cols[5].Trim()));
                Segment3 seg = new Segment3(left, right);
                bsp.Insert(seg);
                _list3.Add(new TerrainOccluder3(left, right));
            }
            sr.Close();
            Draw();
        }

        private void cmdBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string cmd = this.cmdBox.Text;
                string[] cols = cmd.Split(new char[] { ' ' });

                if (cols[0].Trim() == "del")
                {
                    bsp.Clear();
                    List<TerrainOccluder3> list = new List<TerrainOccluder3>();
                    int id = int.Parse(cols[1].Trim());
                    foreach (var item in _list3)
                    {
                        if (id != item.ID)
                        {
                            bsp.Insert(new Segment3(item.Left, item.Right));
                            list.Add(item);
                        }
                    }
                    _list3.Clear();
                    _list3 = list;
                }


                Draw();
            }
        }
    }
}
