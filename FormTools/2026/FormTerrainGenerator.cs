using Common;
using Noise;
using OpenGL;
using Shader;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;
using GlWindow;

namespace FormTools
{
    public partial class FormTerrainGenerator : Form
    {
        readonly string EXE_PATH = Application.StartupPath;

        // 컴퓨트 셰이더들
        NoiseTextureComputeShader _noiseTextureComputeShader;
        WorleyNoiseComputeShader _worleyNoiseComputeShader;
        TerrainHeightmapComputeShader _terrainHeightmapComputeShader;
        DisplayShader _displayShader;

        // 렌더링용 변수
        uint _texture;
        uint _quadVAO;
        uint _quadVBO;
        int _width = 1024;
        int _height = 1024;
        bool _textureReady = false;
        bool _isColorMode = false;

        // 현재 명령어 파라미터
        Dictionary<string, float> _params = new Dictionary<string, float>();

        public FormTerrainGenerator()
        {
            InitializeComponent();
        }

        private void FormTerrainGenerator_Load(object sender, EventArgs e)
        {
            FileHashManager.ROOT_FILE_PATH = StrRes.PROJECT_PATH;

            // 셰이더 초기화
            _noiseTextureComputeShader = new NoiseTextureComputeShader(StrRes.PROJECT_PATH);
            _worleyNoiseComputeShader = new WorleyNoiseComputeShader(StrRes.PROJECT_PATH);
            _terrainHeightmapComputeShader = new TerrainHeightmapComputeShader(StrRes.PROJECT_PATH);
            _displayShader = new DisplayShader(StrRes.PROJECT_PATH);

            CreateFullscreenQuad();

            // 초기 명령어 힌트 표시
            txt_command.Text = "terrain mountain";
            txt_command.ForeColor = Color.Gray;
        }

        private void CreateFullscreenQuad()
        {
            float[] vertices = {
                -1.0f,  1.0f,   0.0f, 1.0f,
                -1.0f, -1.0f,   0.0f, 0.0f,
                 1.0f, -1.0f,   1.0f, 0.0f,

                -1.0f,  1.0f,   0.0f, 1.0f,
                 1.0f, -1.0f,   1.0f, 0.0f,
                 1.0f,  1.0f,   1.0f, 1.0f
            };

            _quadVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_quadVAO);

            _quadVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(sizeof(float) * vertices.Length),
                          vertices, BufferUsage.StaticDraw);

            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false,
                                   4 * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false,
                                   4 * sizeof(float), new IntPtr(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(1);

            Gl.BindVertexArray(0);
        }

        // ============================================================================
        // 명령어 파서
        // ============================================================================

        private void ParseAndExecuteCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            // 명령어를 공백으로 분리
            var parts = command.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            // 파라미터 초기화
            _params.Clear();

            // 파라미터 파싱 (key:value 형식)
            foreach (var part in parts.Skip(1))
            {
                var kv = part.Split(':');
                if (kv.Length == 2)
                {
                    if (float.TryParse(kv[1], out float value))
                    {
                        _params[kv[0]] = value;
                    }
                }
            }

            // 메인 명령어 실행
            string mainCommand = parts[0];

            try
            {
                if (mainCommand == "terrain")
                {
                    GenerateTerrain(parts);
                }
                else if (mainCommand == "noise")
                {
                    GenerateNoise(parts);
                }
                else if (mainCommand == "worley")
                {
                    GenerateWorley(parts);
                }
                else if (mainCommand == "color")
                {
                    ToggleColorMode(parts);
                }
                else if (mainCommand == "save")
                {
                    SaveTexture();
                }
                else if (mainCommand == "help")
                {
                    ShowHelp();
                }
                else if (mainCommand == "clear")
                {
                    txtHistory.Clear();
                }
                else
                {
                    SetStatus($"알 수 없는 명령어: {mainCommand}");
                }

                glControl1.Invalidate();
            }
            catch (Exception ex)
            {
                SetStatus($"오류: {ex.Message}");
            }
        }

        // ============================================================================
        // Terrain 생성
        // ============================================================================

        private void GenerateTerrain(string[] parts)
        {
            // 기본값
            float scale = GetParam("scale", 1.0f);
            int octaves = (int)GetParam("octaves", 6);
            float lacunarity = GetParam("lacunarity", 2.0f);
            float gain = GetParam("gain", 0.5f);
            float heightScale = GetParam("height", 1.0f);
            float roughness = GetParam("roughness", 0.7f);
            int seed = (int)GetParam("seed", 12345);

            // 지형 타입 결정
            int terrainType = 0;
            string typeName = "terrain";

            if (parts.Length > 1)
            {
                string type = parts[1];
                switch (type)
                {
                    case "mountain": terrainType = 5; typeName = "mountain"; break;
                    case "canyon": terrainType = 1; typeName = "canyon"; break;
                    case "volcanic": terrainType = 2; typeName = "volcanic"; break;
                    case "island": terrainType = 3; typeName = "island"; break;
                    case "desert": terrainType = 4; typeName = "desert"; break;
                    default: terrainType = 0; typeName = "terrain"; break;
                }
            }

            // 텍스처 재생성
            RecreateTexture();

            // 생성
            _terrainHeightmapComputeShader.Bind();
            _terrainHeightmapComputeShader.LoadParams(
                _width, _height,
                scale: scale,
                seed: seed,
                offsetX: 0.0f,
                offsetY: 0.0f,
                octaves: octaves,
                lacunarity: lacunarity,
                gain: gain,
                terrainType: terrainType,
                heightScale: heightScale,
                roughness: roughness
            );
            _terrainHeightmapComputeShader.Dispatch(_width, _height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            Gl.Finish();
            _terrainHeightmapComputeShader.Unbind();

            _textureReady = true;

            SetStatus($"생성됨: {typeName} (octaves:{octaves}, roughness:{roughness:F2}, height:{heightScale:F2})");
        }

        // ============================================================================
        // Noise 생성 (Perlin)
        // ============================================================================

        private void GenerateNoise(string[] parts)
        {
            int octaves = (int)GetParam("octaves", 4);
            float scale = GetParam("scale", 1.0f);
            float persistence = GetParam("persistence", 0.5f);
            float lacunarity = GetParam("lacunarity", 2.0f);
            int seed = (int)GetParam("seed", 12345);

            // 모드 결정
            int mode = 0; // 0=Perlin, 1=Perlin FBM
            if (parts.Length > 1 && parts[1] == "fbm")
                mode = 1;

            RecreateTexture();

            _noiseTextureComputeShader.Bind();
            _noiseTextureComputeShader.LoadMode(mode);
            _noiseTextureComputeShader.LoadParams(
                _width, _height,
                scale: scale,
                octaves: octaves,
                persistence: persistence,
                lacunarity: lacunarity,
                seed: seed
            );
            _noiseTextureComputeShader.Dispatch(_width, _height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            Gl.Finish();
            _noiseTextureComputeShader.Unbind();

            _textureReady = true;

            string modeName = mode == 1 ? "Perlin FBM" : "Perlin";
            SetStatus($"생성됨: {modeName} (octaves:{octaves}, scale:{scale:F2})");
        }

        // ============================================================================
        // Worley 생성
        // ============================================================================

        private void GenerateWorley(string[] parts)
        {
            float cellSize = GetParam("cellsize", 0.05f);
            float jitter = GetParam("jitter", 1.0f);
            int octaves = (int)GetParam("octaves", 5);
            float lacunarity = GetParam("lacunarity", 2.0f);
            float gain = GetParam("gain", 0.5f);
            int seed = (int)GetParam("seed", 12345);

            // 모드 결정
            int cloudMode = 0;
            string modeName = "worley";

            if (parts.Length > 1)
            {
                string type = parts[1];
                switch (type)
                {
                    case "fbm": cloudMode = 1; modeName = "worley fbm"; break;
                    case "cloud": cloudMode = 2; modeName = "cloud"; break;
                    case "billowy": cloudMode = 3; modeName = "billowy"; break;
                    case "billowy-fbm": cloudMode = 4; modeName = "billowy fbm"; break;
                    case "ridged": cloudMode = 5; modeName = "ridged"; break;
                    case "ridged-fbm": cloudMode = 6; modeName = "ridged fbm"; break;
                    case "warp": cloudMode = 7; modeName = "domain warped"; break;
                    case "advanced": cloudMode = 8; modeName = "advanced cloud"; break;
                    default: cloudMode = 0; modeName = "worley"; break;
                }
            }

            RecreateTexture();

            _worleyNoiseComputeShader.Bind();
            _worleyNoiseComputeShader.LoadParams(
                _width, _height,
                cellSize: cellSize,
                jitter: jitter,
                distanceType: 0,
                noiseType: 0,
                seed: seed,
                offsetX: 0.0f,
                offsetY: 0.0f,
                octaves: octaves,
                lacunarity: lacunarity,
                gain: gain,
                cloudMode: cloudMode
            );
            _worleyNoiseComputeShader.Dispatch(_width, _height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            Gl.Finish();
            _worleyNoiseComputeShader.Unbind();

            _textureReady = true;

            SetStatus($"생성됨: {modeName} (cellsize:{cellSize:F3}, octaves:{octaves})");
        }

        // ============================================================================
        // 기타 명령
        // ============================================================================

        private void ToggleColorMode(string[] parts)
        {
            if (parts.Length > 1)
            {
                _isColorMode = parts[1] == "on" || parts[1] == "1";
            }
            else
            {
                _isColorMode = !_isColorMode;
            }

            SetStatus($"컬러 모드: {(_isColorMode ? "ON" : "OFF")}");
            glControl1.Invalidate();
        }

        private void SaveTexture()
        {
            if (!_textureReady)
            {
                SetStatus("저장할 텍스처가 없습니다");
                return;
            }

            var image = NoiseTextureComputeShader.SaveToPNG(_texture, _width, _height);
            string filename = $@"C:\Users\mekjh\OneDrive\바탕 화면\terrain_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            image.Save(filename);
            SetStatus($"저장 완료: {filename}");
        }

        private void ShowHelp()
        {
            string help = @"
========== 명령어 도움말 ==========

지형 생성:
  terrain [type] [options]
  - type: mountain, canyon, volcanic, island, desert, terrain
  - 예: terrain mountain octaves:6 roughness:0.8 height:1.2

노이즈 생성:
  noise [type] [options]
  - type: fbm (기본은 perlin)
  - 예: noise fbm octaves:5 scale:1.5

Worley 노이즈:
  worley [type] [options]
  - type: fbm, cloud, billowy, billowy-fbm, ridged, ridged-fbm, warp, advanced
  - 예: worley advanced cellsize:0.05 octaves:6

기타:
  color [on/off]  - 컬러 모드 토글
  save            - 현재 텍스처 저장
  help            - 이 도움말

공통 옵션:
  octaves:N       - FBM 옥타브 수 (기본: 4-6)
  scale:N         - 스케일 (기본: 1.0)
  lacunarity:N    - 주파수 배율 (기본: 2.0)
  gain:N          - 진폭 배율 (기본: 0.5)
  seed:N          - 랜덤 시드 (기본: 12345)
  roughness:N     - 거칠기 0.0-1.0 (지형 전용)
  height:N        - 높이 강도 (지형 전용)
  cellsize:N      - 셀 크기 (Worley 전용)
  jitter:N        - 랜덤성 0.0-1.0 (Worley 전용)
";
            //MessageBox.Show(help, "명령어 도움말", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtHistory.AppendText(help + "\r\n");
        }

        // ============================================================================
        // 유틸리티
        // ============================================================================

        private float GetParam(string key, float defaultValue)
        {
            return _params.ContainsKey(key) ? _params[key] : defaultValue;
        }

        private void SetStatus(string message)
        {
            this.Text = $"Terrain Generator - {message}";
        }

        private void RecreateTexture()
        {
            if (_textureReady && _texture != 0)
            {
                Gl.DeleteTextures(_texture);
            }

            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _texture);
            Gl.TexStorage2D(TextureTarget.Texture2d, 1, InternalFormat.R32f, _width, _height);

            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter,
                           (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter,
                           (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS,
                           (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT,
                           (int)TextureWrapMode.Repeat);

            Gl.BindImageTexture(0, _texture, 0, false, 0, BufferAccess.WriteOnly,
                               InternalFormat.R32f);
        }

        // ============================================================================
        // 렌더링
        // ============================================================================

        private void glControl1_Render(object sender, GlControlEventArgs e)
        {
            if (!_textureReady)
                return;

            Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Viewport(0, 0, glControl1.Width, glControl1.Height);

            _displayShader.Bind();
            _displayShader.LoadNoiseTexture(TextureUnit.Texture0, _texture);

            Gl.BindVertexArray(_quadVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);

            _displayShader.Unbind();
        }

        // ============================================================================
        // 이벤트
        // ============================================================================

        private List<string> cmdHis = new List<string>();  // Queue 대신 List 사용
        private int historyIndex = -1;  // 히스토리 탐색 인덱스

        private void txt_command_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string command = txt_command.Text.Trim();

                if (!string.IsNullOrWhiteSpace(command))
                {
                    this.txtHistory.AppendText($"> {command}\r\n");

                    // 히스토리에 추가 (중복 제거)
                    if (cmdHis.Count == 0 || cmdHis[cmdHis.Count - 1] != command)
                    {
                        cmdHis.Add(command);
                    }

                    // 히스토리 크기 제한 (최대 100개)
                    if (cmdHis.Count > 100)
                    {
                        cmdHis.RemoveAt(0);
                    }

                    historyIndex = cmdHis.Count;  // 인덱스 리셋
                    txt_command.Text = "";
                    ParseAndExecuteCommand(command);
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                // 이전 명령어
                if (cmdHis.Count > 0 && historyIndex > 0)
                {
                    historyIndex--;
                    txt_command.Text = cmdHis[historyIndex];
                    txt_command.SelectionStart = txt_command.Text.Length;  // 커서를 끝으로
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                // 다음 명령어
                if (historyIndex < cmdHis.Count - 1)
                {
                    historyIndex++;
                    txt_command.Text = cmdHis[historyIndex];
                    txt_command.SelectionStart = txt_command.Text.Length;
                }
                else
                {
                    // 히스토리 끝이면 빈 칸
                    historyIndex = cmdHis.Count;
                    txt_command.Text = "";
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txt_command_Enter(object sender, EventArgs e)
        {
            if (txt_command.ForeColor == Color.Gray)
            {
                txt_command.Text = "";
                txt_command.ForeColor = Color.Black;
            }
        }

        private void txt_command_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txt_command.Text))
            {
                txt_command.Text = "명령어 입력 (help 입력 시 도움말)";
                txt_command.ForeColor = Color.Gray;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (_texture != 0)
                Gl.DeleteTextures(_texture);
            if (_quadVAO != 0)
                Gl.DeleteVertexArrays(_quadVAO);
            if (_quadVBO != 0)
                Gl.DeleteBuffers(_quadVBO);
        }
    }
}