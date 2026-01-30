using Common;
using OpenGL;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Terrain;
using ZetaExt;

namespace FormTools
{
    public partial class FormWaterFlow : Form
    {
        readonly string PROJECT_PATH = StrRes.PROJECT_PATH;
        readonly string RES_TERRAIN_PATH = StrRes.PROJECT_PATH + @"\FormTools\bin\Debug\Res\Terrain";

        WaterFlowGenerator _waterFlowGen;
        private uint _frameCount = 0;
        private string _mapTextureName = "";

        public FormWaterFlow()
        {
            InitializeComponent();
            IniFile.s_PATH_ROOT = PROJECT_PATH;
            IniFile.SetFileName($"setup_waterflow.ini");
        }

        private void FormWaterFlow_Load(object sender, EventArgs e)
        {
            // 프로젝트 경로 설정
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;

            // 세이더 및 렌더러 초기화
            //_worleyNoiseComputeShader = new WorleyNoiseComputeShader(StrRes.PROJECT_PATH);

            string mapTexturePath = IniFile.GetPrivateProfileString("Last", "MapTexture", "");
            if (File.Exists(mapTexturePath))
            {
                _waterFlowGen = new WaterFlowGenerator();

                IniFile.WritePrivateProfileString("Last", "MapTexture", mapTexturePath);
                OpenMapTexture(mapTexturePath);
                this.Invalidate();
                this.glControl1.Focus();
            }
        }

        public void ConsoleWrite(string text)
        {
            this.txtPrint.Text += text + "\r\n";
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void 열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.InitialDirectory = RES_TERRAIN_PATH;
            this.openFileDialog1.Filter = "PNG 파일 (*.png)|*.png|모든 파일 (*.*)|*.*";
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (_waterFlowGen == null)
                    _waterFlowGen = new WaterFlowGenerator();

                OpenMapTexture(this.openFileDialog1.FileName);
                IniFile.WritePrivateProfileString("Last", "MapTexture", this.openFileDialog1.FileName);
            }
        }

        private void glControl1_Render(object sender, GlControlEventArgs e)
        {
            Gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Viewport(0, 0, glControl1.Width, glControl1.Height);

            // 재생이 체크되면
            if (chk_play.Checked)
            {
                float rainAmount = sld_rain_amout.Value * 0.0001f;

                _waterFlowGen?.Move(sld_velocity.Value * 0.1f, sld_evaporationFactor.Value * 0.001f);

                if (chk_auto_rain.Checked)
                {
                    if (_frameCount % 100 == 0)
                    {
                        _waterFlowGen?.RunAddWater(rainAmount);
                    }
                }
            }

            // 렌더링
            _waterFlowGen?.Render(0.01f, this.sld_color_scaled.Value, chk_useHeightMap.Checked);
        }

        public void OpenMapTexture(string filePath)
        {
            if (File.Exists(filePath))
            {
                _mapTextureName = Path.GetFileNameWithoutExtension(filePath);
                Image bitmap = Image.FromFile(filePath);
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                this.picOriginal.Image = bitmap;
                this.txtPrint.Text += _waterFlowGen.Load(filePath);
                this.Invalidate();
                this.Text = "Water Flow Simulation - " + Path.GetFileName(filePath);
            }
        }


        private void 초기화ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _waterFlowGen.Clear();
            this.txtPrint.Text += "초기화 완료";
        }

        private void glControl1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (MessageBox.Show("프로그램을 종료하시겠습니까?", "종료 확인", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            }
            else if (e.KeyCode == Keys.Space)
            {
                chk_play.Checked = !chk_play.Checked;
            }
        }

        private void 실행ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            _waterFlowGen.RunRandomAddWater();
            this.txtPrint.Text += "랜덤 물뿌리기 완료";
        }

        private void gPU에서가져오기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            float scale = this.sld_color_scaled.Value;
            this.picReadBuffer.Image = _waterFlowGen.ExportWaterMapToPNG(_waterFlowGen.ReadBuffer);
            this.picWriteBuffer.Image = _waterFlowGen.ExportWaterMapToPNG(_waterFlowGen.WriteBuffer);
        }

        private void 중앙에물뿌리기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _waterFlowGen.RunAddWater();
            this.txtPrint.Text += "중앙 물뿌리기 완료";
        }

        private void 테스트ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void sld_color_scaled_DragLeave(object sender, EventArgs e)
        {
            float scale = this.sld_color_scaled.Value;
            this.picReadBuffer.Image = _waterFlowGen.ExportWaterMapToPNG(_waterFlowGen.ReadBuffer);
            this.picWriteBuffer.Image = _waterFlowGen.ExportWaterMapToPNG(_waterFlowGen.WriteBuffer);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            this.picReadBuffer.Image = _waterFlowGen.ExportWaterMapToPNG(_waterFlowGen.ReadBuffer, true);
            this.picWriteBuffer.Image = _waterFlowGen.ExportWaterMapToPNG(_waterFlowGen.WriteBuffer, true);

            Bitmap bitmap = (Bitmap)this.picReadBuffer.Image;
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            this.picReadBuffer.Image.Save(RES_TERRAIN_PATH + $"\\{_mapTextureName}_river.png");
        }
    }
}