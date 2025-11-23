using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExtractIncludeGLSL
{
    public partial class Form1 : Form
    {
        string PATH = "C:\\Users\\mekjh\\OneDrive\\바탕 화면\\OpenEng3d\\Shader\\common\\";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.InitialDirectory = PATH;

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Open(this.openFileDialog1.FileName);
            }
        }

        public void Open(string fileName)
        {
            this.textBox1.Clear();
            this.label1.Text = fileName;

            List<string> funcs = new List<string>();

            List<string> comts = new List<string>();
            string result = "";
            string fileNameWithoutPath = Path.GetFileName(fileName);
            string dir = Path.GetDirectoryName(fileName);

            // 인클루드 파일이 있으면
            if (File.Exists(fileName))
            {
                // 인클루드 파일을 가져온다.
                string inc = File.ReadAllText(fileName);

                // 구조체를 모두 찾아서 제거한다.
                int start = 0;
                while (start >= 0)
                {
                    start = inc.IndexOf("struct ");
                    if (start >= 0)
                    {
                        int end = inc.IndexOf("};", start);
                        if (end > start)
                        {
                            inc = inc.Substring(0, start).TrimEnd() + inc.Substring(end + 2).TrimStart();
                            end = 0;
                        }
                        else
                        {
                            throw new Exception($"glsl struct 구문 오류입니다. {fileName}");
                        }
                    }
                };

                result += inc;

                // 함수선언부를 모두 찾아 리스트에 담는다.
                start = 0;
                string[] lines = result.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (!line.StartsWith("\t") && !line.StartsWith(" ") && !line.StartsWith("{") && !line.StartsWith("}")
                        && !line.StartsWith("//") && !line.StartsWith("#version")
                        )
                    {
                        if (line.TrimEnd().EndsWith(")") || line.TrimEnd().EndsWith("{"))
                        {
                            string funcDeclared = line.Replace("{", "").Trim();
                            if (funcDeclared != "") funcs.Add(funcDeclared.Replace("  ", "") + ";");
                            if (i > 0)
                            {
                                string prev = lines[i - 1];
                                comts.Add(prev.StartsWith("//") ? "\r\n" + prev : "");
                            }
                            else
                            {
                                comts.Add("");
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < funcs.Count; i++)
            {
                //this.textBox1.AppendText(comts[i] + "\r\n");
                this.textBox1.AppendText(funcs[i] + "\r\n");
            }

            Clipboard.SetText(this.textBox1.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.listBox1.Items.Clear();
            string[] files = Directory.GetFiles(PATH);
            for (int i = 0; i < files.Length; i++)
            {
                string fn = Path.GetFileName(files[i]);
                this.listBox1.Items.Add(fn);
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Open(PATH + "\\" + this.listBox1.Text);
        }
    }
}