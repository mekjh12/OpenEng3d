using Common;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Terrain;
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

        private uint _baseHeightTextureId = 0;

        public FormTerrainGen()
        {
            InitializeComponent();
            IniFile.s_PATH_ROOT = PROJECT_PATH;
            IniFile.SetFileName($"setup_terrain_gen.ini");

            string filename = IniFile.GetPrivateProfileString("map", "dir", @"C:\Users\Public\Documents");
            if (Directory.Exists(filename))
            {
                this.fileSystemWatcher1.Path = filename;
            }
        }

        private void FormTerrainGen_Load(object sender, EventArgs e)
        {
            // 프로젝트 경로 설정
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;

            // 파일 감시자 초기화
            this.label1.Text = this.fileSystemWatcher1.Path;
            fileSystemWatcher1_Changed(null, null);

            // 높이맵 생성기 초기화
            _generator = new HeightmapGenerator();
            _generator.Initialize(StrRes.PROJECT_PATH, size: 1025);
            _generator.Generate(_baseHeightTextureId, true, true);
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
            _generator.Generate(_baseHeightTextureId);
        }

        private void sld_octaves_ValueChanged(object sender, EventArgs e)
        {
            _generator.Generate(_baseHeightTextureId);
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
                   
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

        private void pic_heightmap_Click(object sender, EventArgs e)
        {
            string filename = RES_TERRAIN_PATH + $"\\{this.txtCoord.Text}.png";
            if (File.Exists(filename))
            {
                this.pic_heightmap.Image = new Bitmap(filename);
            }
        }

        private void pic_normal_Click(object sender, EventArgs e)
        {
            string heightMapFileName = RES_TERRAIN_PATH + $"\\{this.txtCoord.Text}.png";
            string filename = RES_TERRAIN_PATH + $"\\{this.txtCoord.Text}_normal.png";
            if (File.Exists(filename))
            {
                this.pic_normal.Image = new Bitmap(filename);
            }
            else
            {
                if (MessageBox.Show("노말맵이 존재하지 않습니다. 생성하시겠습니까?", "노말맵 생성 확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // 지형 노말맵 생성
                    uint normalMapTexture = NormalMapGenerator.GenerateNormalMap(
                        heightMapFileName,
                        heightScale: Constants.TERRAIN_VERTICAL_SCALE,
                        wrapMode: true
                    );
                    pic_normal_Click(sender, e);
                }
            }
        }

        private void pic_river_Click(object sender, EventArgs e)
        {
            string filename = RES_TERRAIN_PATH + $"\\{this.txtCoord.Text}_river.png";
            if (File.Exists(filename))
            {
                this.pic_river.Image = new Bitmap(filename);
            }
        }

        private void pic_normal_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (MessageBox.Show("삭제하시겠습니까?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string filename = RES_TERRAIN_PATH + $"\\{this.txtCoord.Text}_normal.png";
                if (File.Exists(filename))
                {
                    this.pic_normal.Image.Dispose();
                    this.pic_normal.Image = null;
                    File.Delete(filename);
                }
            }
        }

        private void 지형보기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FormTerrainRegion().Show();
        }

        private void 지형이미지타일링하기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private List<string> TileSplit(string fileName, int offsetX, int offsetY)
        {
            List<string> list = new List<string>();

            // 1. 저장할 폴더 생성 (이미지 파일명과 동일한 폴더)
            string outputDir = Path.Combine(Path.GetDirectoryName(fileName), "Tiles");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            try
            {
                // 2. 원본 이미지 로드
                using (Bitmap sourceImage = new Bitmap(fileName))
                {
                    int tileSize = 120;
                    int columns = 9;
                    int rows = 9;

                    sourceImage.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    // 3. 이중 루프를 돌며 타일 절단 및 저장
                    for (int y = 0; y < rows; y++)
                    {
                        for (int x = 0; x < columns; x++)
                        {
                            // 타일 영역 설정
                            Rectangle tileSrcRect = new Rectangle(x * tileSize, y * tileSize, tileSize + 1, tileSize + 1);

                            // 타일 생성 및 그리기
                            using (Bitmap tile = new Bitmap(tileSize, tileSize))
                            {
                                using (Graphics g = Graphics.FromImage(tile))
                                {
                                    g.DrawImage(sourceImage, 
                                        destRect: new Rectangle(0, 0, tileSize, tileSize), 
                                        srcRect: tileSrcRect, GraphicsUnit.Pixel);
                                }

                                // 4. 파일 저장 (png 형식 권장)
                                int cordX = x + offsetX;
                                int cordY = y + offsetY;
                                string outputFileName = $"tile_{cordX}_{cordY}.png";
                                tile.RotateFlip(RotateFlipType.RotateNoneFlipY);
                                tile.Save(Path.Combine(outputDir, outputFileName), ImageFormat.Png);

                                list.Add(Path.Combine(outputDir, outputFileName));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}");
            }

            return list;
        }

        private void 지형이미지다듬기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filepath = this.openFileDialog1.FileName;
                LoadBaseMap(filepath);
            }
        }

        private void LoadBaseMap(string filename)
        {
            // 1. 파일명에서 좌표 추출 로직
            string fileNameOnly = Path.GetFileNameWithoutExtension(filename);
            string[] parts = fileNameOnly.Split('_');

            if (parts.Length >= 3 && parts[0] == "tile")
            {
                if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                {
                    //Console.WriteLine($"추출된 좌표 -> 열(X): {x}, 행(Y): {y}");

                    // 2. 비트맵 로드 (using을 사용하여 처리가 끝나면 즉시 메모리 해제)
                    using (Bitmap bitmap = (Bitmap)Bitmap.FromFile(filename))
                    {
                        // 상하를 반전한다.
                        // OpenGL은 텍스처 좌표계가 왼쪽 아래가 원점이지만,
                        // 일반적인 이미지 파일은 왼쪽 위가 원점이기 때문에 이를 맞춰주기 위함입니다.
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                        // 3. 텍스처 ID 관리 (중복 생성 방지)
                        if (_baseHeightTextureId == 0)
                        {
                            _baseHeightTextureId = Gl.GenTexture();
                            Gl.BindTexture(TextureTarget.Texture2d, _baseHeightTextureId);

                            // 최초 생성 시에만 텍스처 파라미터 설정
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMinFilter.Linear);
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                        }
                        else
                        {
                            // 이미 ID가 있다면 기존 텍스처 바인딩
                            Gl.BindTexture(TextureTarget.Texture2d, _baseHeightTextureId);
                        }

                        // 4. GPU로 데이터 전송
                        BitmapData data = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                        try
                        {
                            // 기존 메모리 공간에 새로운 이미지 데이터를 덮어씌움
                            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba,
                                data.Width, data.Height, 0,
                                OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                        }
                        finally
                        {
                            bitmap.UnlockBits(data);
                        }

                        // 5. GPU 전송 완료 대기 (선택 사항이지만 연속 루프 시 안정적임)
                        Gl.Flush();
                    }
                }
                else
                {
                    MessageBox.Show("파일명의 숫자 형식이 올바르지 않습니다.");
                }
            }
            else
            {
                MessageBox.Show("타일 이미지 파일명이 규칙(tile_y_x.png)에 맞지 않습니다.");
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filepath = this.openFileDialog1.FileName;
                LoadBaseMap(filepath);
                string[] strings = Path.GetFileNameWithoutExtension(filepath).Split('_');
                this.txtCoord.Text = strings[1] + "x" + strings[2];
            }
        }

        private void fileSystemWatcher1_Changed(object sender, FileSystemEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => fileSystemWatcher1_Changed(sender, e)));
                return;
            }
        }

        private void btnFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "폴더를 선택하세요.";

                // 초기화 경로 지정
                dialog.SelectedPath = @"C:\Users\Public\Documents";

                // "새 폴더 만들기" 버튼 표시 여부
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = dialog.SelectedPath;
                    Console.WriteLine($"선택된 경로: {folderPath}");
                    this.fileSystemWatcher1.Path = folderPath;
                    this.label1.Text = this.fileSystemWatcher1.Path;
                    fileSystemWatcher1_Changed(null, null);
                    IniFile.WritePrivateProfileString("map", "dir", folderPath);
                }
            }
        }

        private void btnAutoMacro_Click(object sender, EventArgs e)
        {
            string folder = this.label1.Text;

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

            foreach (var item in Directory.GetFiles(folder))
            {
                string fileName = Path.GetFileName(item);
                string extension = Path.GetExtension(item).ToLower();

                if (extension == ".png")
                {
                    string[] strings = Path.GetFileNameWithoutExtension(fileName).Split('_');
                    if (strings.Length == 3)
                    {
                        string rawFile = folder + "\\" + strings[1] + "x" + strings[2] + ".raw";
                        bool hasRawFile = File.Exists(rawFile);

                        LoadBaseMap(item);
                        _generator.Generate(_baseHeightTextureId);
                        _generator.SaveHeightmapToPng(rawFile);
                    }
                }
            }
        }

        private void 지형이미지업스케일ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.InitialDirectory = this.label1.Text;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string folderPath = openFileDialog1.FileName;
                string outputFolder = Path.GetDirectoryName(folderPath) + @"\output\";
                TiledImageUpscaler.ProcessTiledUpscale(folderPath, outputFolder);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            this.txtConsole.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<string> tileList = null;
            int offsetX = 0;
            int offsetY = 0;

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = Path.GetFileNameWithoutExtension(this.openFileDialog1.FileName);
                string[] cols = fileName.Split('x');
                if (cols.Length != 2)
                {
                    MessageBox.Show($"Offset Tiles은 0x0인 형식이어야 합니다.");
                    return;
                }

                offsetX = 9 * int.Parse(cols[0].Trim());
                offsetY = 9 * int.Parse(cols[1].Trim());

                // 타일 이미지 절단 및 저장
                tileList = TileSplit(this.openFileDialog1.FileName, offsetX, offsetY);
            }

            // 타일 이미지 일괄 처리
            foreach (var path in tileList)
            {
                // 1. CPU -> GPU: 텍스처 전송 (LoadBaseMap)
                LoadBaseMap(path);

                // 2. GPU 연산 시작
                _generator.Generate(_baseHeightTextureId, useBicubic: true, useGaussianBlur: true);

                // 3.GPU가 Generate 연산을 끝낼 때까지 CPU가 여기서 대기합니다.
                Gl.Finish();

                // 4. GPU -> CPU -> Disk: 결과 읽기 및 저장
                _generator.SaveHeightmapToPng(Path.ChangeExtension(path, "raw"));
            }

            txtConsole.AppendText("타일 이미지 일괄 처리 완료");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = (this.openFileDialog1.FileName);

                // 1. CPU -> GPU: 텍스처 전송 (LoadBaseMap)
                LoadBaseMap(fileName);

                // 2. GPU 연산 시작, 하이트맵 생성 파이프라인 (Bilinear -> Bicubic -> Gaussian Blur)
                _generator.Generate(_baseHeightTextureId, useBicubic: true, useGaussianBlur: true);

                // 3.GPU가 Generate 연산을 끝낼 때까지 CPU가 여기서 대기합니다.
                Gl.Finish();

                _generator.SaveHeightmapToPng(Path.ChangeExtension("", "raw"));

            }
        }
    }
}
