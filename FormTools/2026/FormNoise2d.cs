using Common;
using Noise;
using OpenGL;
using Shader;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormNoise2d : Form
    {
        readonly string EXE_PATH = Application.StartupPath;

        NoiseTextureComputeShader _noiseTextureComputeShader;
        WorleyNoiseComputeShader _worleyNoiseComputeShader;

        DisplayShader _displayShader;

        // 렌더링용 변수
        uint _noiseTexture;
        uint _quadVAO;
        uint _quadVBO;
        int _width = 1024;
        int _height = 1024;
        bool _textureReady = false;
        bool _isColorMode = false;

        public FormNoise2d()
        {
            InitializeComponent();
        }

        private void FormNoise2d_Load(object sender, EventArgs e)
        {
            // 파일 해시 매니저 초기화
            FileHashManager.ROOT_FILE_PATH = StrRes.PROJECT_PATH;

            // 노이즈 텍스처 컴퓨트 셰이더 초기화
            _noiseTextureComputeShader = new NoiseTextureComputeShader(StrRes.PROJECT_PATH);
            _worleyNoiseComputeShader = new WorleyNoiseComputeShader(StrRes.PROJECT_PATH);

            // 디스플레이 셰이더 초기화
            _displayShader = new DisplayShader(StrRes.PROJECT_PATH);

            // 풀스크린 쿼드 생성
            CreateFullscreenQuad();

            // 초기 노이즈 생성
            GenerateNoise();
        }

        private void CreateFullscreenQuad()
        {
            // 풀스크린 쿼드 정점 데이터 (위치 + 텍스처 좌표)
            float[] vertices = {
                // 위치(x,y)     텍스처좌표(u,v)
                -1.0f,  1.0f,   0.0f, 1.0f,  // 왼쪽 상단
                -1.0f, -1.0f,   0.0f, 0.0f,  // 왼쪽 하단
                 1.0f, -1.0f,   1.0f, 0.0f,  // 오른쪽 하단
                
                -1.0f,  1.0f,   0.0f, 1.0f,  // 왼쪽 상단
                 1.0f, -1.0f,   1.0f, 0.0f,  // 오른쪽 하단
                 1.0f,  1.0f,   1.0f, 1.0f   // 오른쪽 상단
            };

            // VAO 생성
            _quadVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_quadVAO);

            // VBO 생성
            _quadVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(sizeof(float) * vertices.Length),
                          vertices, BufferUsage.StaticDraw);

            // 정점 속성 설정
            // 위치 속성
            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false,
                                   4 * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            // 텍스처 좌표 속성
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false,
                                   4 * sizeof(float), new IntPtr(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(1);

            Gl.BindVertexArray(0);
        }

        public void GenerateNoise()
        {
            // 기존 텍스처 삭제
            if (_textureReady && _noiseTexture != 0)
            {
                Gl.DeleteTextures(_noiseTexture);
            }

            // 슬라이더 값 읽기
            int octaves = this.sld_octaves.Value;               // 1~ 10
            float scale = this.sld_scale.Value * 0.1f;          // 0.1 ~ 10.0
            float persistence = this.sld_persistence.Value * 0.01f;  // 0.1 ~ 1.0
            float lacunarity = this.sld_lacunarity.Value * 0.01f;  // 1.0 ~ 5.0

            // 텍스처 생성
            _noiseTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _noiseTexture);
            Gl.TexStorage2D(TextureTarget.Texture2d, 1, InternalFormat.R32f, _width, _height);

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter,
                           (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter,
                           (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS,
                           (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT,
                           (int)TextureWrapMode.ClampToEdge);

            // 이미지 바인딩
            Gl.BindImageTexture(0, _noiseTexture, 0, false, 0, BufferAccess.WriteOnly,
                               InternalFormat.R32f);

            // 셰이더 실행
            _noiseTextureComputeShader.Bind();
            _noiseTextureComputeShader.LoadMode(cmb_mode.SelectedIndex);
            _noiseTextureComputeShader.LoadParams(
                _width, _height,
                scale: scale,
                octaves: octaves,
                persistence: persistence,
                lacunarity: lacunarity,
                seed: 12345
            );
            _noiseTextureComputeShader.Dispatch(_width, _height);

            // GPU 작업 완료 대기
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            Gl.Finish();
            _noiseTextureComputeShader.Unbind();

            _textureReady = true;
        }

        public void WorleyNoise()
        {
            if (_textureReady && _noiseTexture != 0)
            {
                Gl.DeleteTextures(_noiseTexture);
            }

            // 슬라이더 값 읽기
            int octaves = this.worley_octaves.Value;               // 1~ 10
            float cellsize = this.worley_cellsize.Value * 0.0005f;
            float lacunarity = this.worley_lacunarity.Value * 0.01f;  // 1.0 ~ 5.0
            float gain = this.worley_gain.Value * 0.1f;  // 0.1 ~ 1.0

            _noiseTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _noiseTexture);
            Gl.TexStorage2D(TextureTarget.Texture2d, 1, InternalFormat.R32f, _width, _height);

            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter,
                           (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter,
                           (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS,
                           (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT,
                           (int)TextureWrapMode.Repeat);

            Gl.BindImageTexture(0, _noiseTexture, 0, false, 0, BufferAccess.WriteOnly,
                               InternalFormat.R32f);

            _worleyNoiseComputeShader.Bind();
            _worleyNoiseComputeShader.LoadParams(
                _width, _height,
                cellSize: cellsize,
                jitter: 1.0f,
                distanceType: 0,
                noiseType: 0,
                seed: 12345,
                offsetX: 0.0f,
                offsetY: 0.0f,
                octaves: octaves,
                lacunarity: lacunarity,
                gain: gain,
                cloudMode: cmb_worley.SelectedIndex  // 0=Worley, 1=Worley-FBM, 2=Cloud
            );
            _worleyNoiseComputeShader.Dispatch(_width, _height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            Gl.Finish();
            _worleyNoiseComputeShader.Unbind();

            this.toolStripStatusLabel1.Text = "Worley 노이즈 생성 완료" + (cmb_mode.SelectedIndex);
            _textureReady = true;
        }

        private void glControl1_Render(object sender, GlControlEventArgs e)
        {
            if (!_textureReady)
                return;

            // 화면 클리어
            Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            // 뷰포트 설정
            Gl.Viewport(0, 0, glControl1.Width, glControl1.Height);

            // 셰이더 바인딩 및 텍스처 설정
            _displayShader.Bind();
            _displayShader.LoadNoiseTexture(TextureUnit.Texture0, _noiseTexture);
            _displayShader.LoadFlip(this.chkFlip.Checked);
            _displayShader.LoadUseColorMap(_isColorMode);

            // 쿼드 그리기
            Gl.BindVertexArray(_quadVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);

            _displayShader.Unbind();
        }

        // 폼 종료 시 리소스 정리
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (_noiseTexture != 0)
                Gl.DeleteTextures(_noiseTexture);
            if (_quadVAO != 0)
                Gl.DeleteVertexArrays(_quadVAO);
            if (_quadVBO != 0)
                Gl.DeleteBuffers(_quadVBO);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void sld_octaves_ValueChanged(object sender, EventArgs e)
        {
            GenerateNoise();
            glControl1.Invalidate(); // 화면 갱신
        }

        private void sld_scale_ValueChanged(object sender, EventArgs e)
        {
            GenerateNoise();
            glControl1.Invalidate(); // 화면 갱신
            sld_scale.SetText((sld_scale.Value * 0.1f).ToString("#.#"));
        }

        private void sld_persistence_ValueChanged(object sender, EventArgs e)
        {
            GenerateNoise();
            glControl1.Invalidate(); // 화면 갱신
            sld_persistence.SetText((sld_persistence.Value * 0.01f).ToString("#.##"));
        }

        private void sld_lacunarity_ValueChanged(object sender, EventArgs e)
        {
            GenerateNoise();
            glControl1.Invalidate(); // 화면 갱신
            sld_lacunarity.SetText((sld_lacunarity.Value * 0.01f).ToString("#.##"));
        }

        private void 저장ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_textureReady)
            {
                var image = NoiseTextureComputeShader.SaveToPNG(_noiseTexture, _width, _height);
                image.Save(@"C:\Users\mekjh\OneDrive\바탕 화면\noise_output.png");
                toolStripStatusLabel1.Text = "저장 완료: noise_output.png";
            }
        }

        private void chk_color_CheckedChanged(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = this.chk_color.Checked ? "컬러 모드" : "흑백 모드";
            _isColorMode = this.chk_color.Checked;
            glControl1.Invalidate(); // 화면 갱신
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cmb_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            GenerateNoise();
            glControl1.Invalidate(); // 화면 갱신
        }

        private void worely_lacunarity_Load(object sender, EventArgs e)
        {
            
        }

        private void worely_lacunarity_ValueChanged(object sender, EventArgs e)
        {
            WorleyNoise();
            glControl1.Invalidate(); // 화면 갱신
            worley_cellsize.SetText((worley_cellsize.Value * 0.01f).ToString("#.##"));
        }

        private void cmb_worley_SelectedIndexChanged(object sender, EventArgs e)
        {
            WorleyNoise();
            glControl1.Invalidate(); // 화면 갱신
        }

        private void worley_lacunarity_ValueChanged(object sender, EventArgs e)
        {
            WorleyNoise();
            glControl1.Invalidate(); // 화면 갱신
        }

        private void worley_octaves_ValueChanged(object sender, EventArgs e)
        {
            WorleyNoise();
            glControl1.Invalidate(); // 화면 갱신
        }

        private void chkFlip_CheckedChanged(object sender, EventArgs e)
        {
            glControl1.Invalidate(); // 화면 갱신
        }
    }
}