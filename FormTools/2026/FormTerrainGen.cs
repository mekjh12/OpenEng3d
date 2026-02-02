using Common;
using OpenGL;
using Shader;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormTerrainGen : Form
    {
        readonly string PROJECT_PATH = StrRes.PROJECT_PATH;
        readonly string RES_TERRAIN_PATH = StrRes.PROJECT_PATH + @"\FormTools\bin\Debug\Res\Terrain";

        private uint _frameCount = 0;
        private string _mapTextureName = "";

        HeightmapGenerator _generator;
        DisplayShader _displayShader;

        uint _heightmapTexture;


        public FormTerrainGen()
        {
            InitializeComponent();
            IniFile.s_PATH_ROOT = PROJECT_PATH;
            IniFile.SetFileName($"setup_terrain_gen.ini");
        }

        private void FormTerrainGen_Load(object sender, EventArgs e)
        {
            // 프로젝트 경로 설정
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;

            // 높이맵 생성기 초기화
            _generator = new HeightmapGenerator();
            _generator.Initialize(StrRes.PROJECT_PATH, size: 2048);
            _generator.Generate(2, 5);
            _heightmapTexture = _generator.HeightMapTexture;
        }


        private void glControl1_Render(object sender, GlControlEventArgs e)
        {
            int w = glControl1.Width;
            int h = glControl1.Height;

            Gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Viewport(0, 0, w, h);

            _generator.Render(0.01f);
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
        }

        private void sld_scale_ValueChanged(object sender, EventArgs e)
        {
            _generator.Generate(sld_scale.Value * 0.01f, sld_octaves.Value);
        }

        private void sld_octaves_ValueChanged(object sender, EventArgs e)
        {
            _generator.Generate(sld_scale.Value * 0.01f, sld_octaves.Value);
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            string filename = RES_TERRAIN_PATH + @"\heightmap_generated.png";
            _generator.SaveHeightmapToPng(filename);
            if (File.Exists(filename))
            {
                MessageBox.Show($"높이맵이 '{filename}'(으)로 저장되었습니다.", "저장 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }            
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void btn_refresh_Click(object sender, EventArgs e)
        {
            ExistFileMapTextures();
        }

        private void ExistFileMapTextures()
        {
            string zoneName = this.txtCoord.Text;
            pic_heightmap.Image = GetTextureMap(RES_TERRAIN_PATH + $@"\{zoneName}.png");
            pic_normal.Image = GetTextureMap(RES_TERRAIN_PATH + $@"\{zoneName}_normal.png");
            pic_river.Image = GetTextureMap(RES_TERRAIN_PATH + $@"\{zoneName}_river.png");
            Bitmap GetTextureMap(string filename)
            {
                if (File.Exists(filename))
                {
                    return new Bitmap(filename);
                }
                return null;
            }
        }

        private void 읽어오기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            float[] heights = PngHandler.Load(RES_TERRAIN_PATH + @"\0x0.png", out int width, out int height);

            PngHandler.SaveRgb16(RES_TERRAIN_PATH + @"\0x0_saved.png", heights, width, height);
        }
    }
}
