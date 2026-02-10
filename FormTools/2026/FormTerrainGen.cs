using Common;
using OpenGL;
using Shader;
using System;
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
            _generator.Generate(_baseHeightTextureId, chk_first.Checked, chk_second.Checked);
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
            _generator.Generate(_baseHeightTextureId,chk_first.Checked, chk_second.Checked);
        }

        private void sld_octaves_ValueChanged(object sender, EventArgs e)
        {
            _generator.Generate(_baseHeightTextureId, chk_first.Checked, chk_second.Checked);
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
            string[] cols = this.txtTileOffset.Text.Split(',');
            if (cols.Length != 2) MessageBox.Show($"Offset Tiles은 0,0인 형식이어야 합니다.");

            int offsetX = int.Parse(cols[0].Trim());
            int offsetY = int.Parse(cols[1].Trim());

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Console.WriteLine("https://heightmap.skydark.pl/");
                string filepath = this.openFileDialog1.FileName;

                // 1. 저장할 폴더 생성 (이미지 파일명과 동일한 폴더)
                string outputDir = Path.Combine(Path.GetDirectoryName(filepath), "Tiles");
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                try
                {
                    // 2. 원본 이미지 로드
                    using (Bitmap sourceImage = new Bitmap(filepath))
                    {
                        int tileSize = 120;
                        int columns = 9;
                        int rows = 9;

                        // 3. 이중 루프를 돌며 타일 절단 및 저장
                        for (int y = 0; y < rows; y++)
                        {
                            for (int x = 0; x < columns; x++)
                            {
                                // 타일 영역 설정
                                Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize + 1, tileSize + 1);

                                // 타일 생성 및 그리기
                                using (Bitmap tile = new Bitmap(tileSize, tileSize))
                                {
                                    using (Graphics g = Graphics.FromImage(tile))
                                    {
                                        g.DrawImage(sourceImage, new Rectangle(0, 0, tileSize, tileSize), tileRect, GraphicsUnit.Pixel);
                                    }

                                    // 4. 파일 저장 (png 형식 권장)
                                    int cordX = x + offsetX;
                                    int cordY = y + offsetY;
                                    string fileName = $"tile_{cordY}_{cordX}.png";
                                    tile.Save(Path.Combine(outputDir, fileName), ImageFormat.Png);
                                }
                            }
                        }
                    }
                    MessageBox.Show($"타일 생성이 완료되었습니다!\n저장 위치: {outputDir}", "완료");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"오류 발생: {ex.Message}");
                }
            }
        }

        private uint _baseHeightTextureId = 0;

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
            // 1. 확장자를 제외한 파일명만 가져오기 (예: "tile_2_5")
            string fileNameOnly = Path.GetFileNameWithoutExtension(filename);

            // 2. 언더바(_)를 기준으로 문자열 분리
            string[] parts = fileNameOnly.Split('_');

            // 3. 배열 크기 확인 및 데이터 추출
            // parts[0] = "tile", parts[1] = "y", parts[2] = "x"
            if (parts.Length >= 3 && parts[0] == "tile")
            {
                if (int.TryParse(parts[1], out int y) && int.TryParse(parts[2], out int x))
                {
                    // 파싱 성공! 여기서 x, y를 활용해 다듬기 로직을 수행하세요.
                    Console.WriteLine($"추출된 좌표 -> 행(Y): {y}, 열(X): {x}");

                    // 읽어온 파일 GPU로 전송
                    Bitmap bitmap = (Bitmap)Bitmap.FromFile(filename);

                    int width = bitmap.Width;
                    int height = bitmap.Height;

                    _baseHeightTextureId = Gl.GenTexture();
                    Gl.BindTexture(TextureTarget.Texture2d, _baseHeightTextureId);

                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, data.Width, data.Height, 0,
                         OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMinFilter.Linear);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);

                    bitmap.UnlockBits(data);
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
        private void sld_scale_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            LoadBaseMap(@"C:\Users\mekjh\Videos\Downloads\heightmap_Tiles\tile_4_5.png");
            _generator.Generate(_baseHeightTextureId, chk_first.Checked, chk_second.Checked);
        }

        private void chk_first_CheckedChanged(object sender, EventArgs e)
        {
            _generator.Generate(_baseHeightTextureId, chk_first.Checked, chk_second.Checked);

        }

        private void chk_second_CheckedChanged(object sender, EventArgs e)
        {
            _generator.Generate(_baseHeightTextureId, chk_first.Checked, chk_second.Checked);
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

            // 1. 현재 상태 저장 (스크롤 위치와 선택된 항목의 이름)
            int oldTopIndex = checkedListBox1.TopIndex;
            string selectedItemText = checkedListBox1.SelectedItem?.ToString();

            this.checkedListBox1.Items.Clear();
            string folder = this.label1.Text;

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

            // 2. 아이템 다시 채우기
            foreach (var item in Directory.GetFiles(folder))
            {
                string fileName = Path.GetFileName(item);
                string extension = Path.GetExtension(item).ToLower();

                if (extension == ".png")
                {
                    string[] strings = Path.GetFileNameWithoutExtension(fileName).Split('_');
                    if (strings.Length == 3)
                    {
                        string rawFile = Path.Combine(folder, strings[1] + "x" + strings[2] + ".raw");
                        bool hasRawFile = File.Exists(rawFile);
                        this.checkedListBox1.Items.Add(fileName, hasRawFile);
                    }
                }
            }

            // 3. 상태 복구
            // 이전에 선택했던 파일명이 리스트에 있다면 다시 선택해줌
            if (!string.IsNullOrEmpty(selectedItemText))
            {
                int newIndex = checkedListBox1.FindStringExact(selectedItemText);
                if (newIndex != -1)
                {
                    checkedListBox1.SelectedIndex = newIndex;
                }
            }

            // 4. 스크롤 위치 복구 (가장 중요!)
            if (oldTopIndex < checkedListBox1.Items.Count)
            {
                checkedListBox1.TopIndex = oldTopIndex;
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

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] strings = Path.GetFileNameWithoutExtension(this.checkedListBox1.Text).Split('_');
            this.txtCoord.Text = strings[1] + "x" + strings[2];
            LoadBaseMap(this.label1.Text + "\\" + this.checkedListBox1.Text);
            _generator.Generate(_baseHeightTextureId, chk_first.Checked, chk_second.Checked);
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
                        _generator.Generate(_baseHeightTextureId, chk_first.Checked, chk_second.Checked);
                        _generator.SaveHeightmapToPng(rawFile);
                    }
                }
            }

        }

        private void checkedListBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // 1. 더블클릭한 위치의 인덱스를 정확히 가져옵니다.
            int index = checkedListBox1.IndexFromPoint(e.Location);

            // 항목이 없는 빈 공간을 클릭했다면 중단
            if (index == ListBox.NoMatches) return;

            // 2. 파일 저장 로직 수행
            string filename = Path.Combine(this.fileSystemWatcher1.Path, $"{this.txtCoord.Text}.raw");
            _generator.SaveHeightmapToPng(filename);

            if (File.Exists(filename))
            {
                this.statusStrip1.Items[0].Text = $"저장 완료: {filename}";

                // 3. 핵심: 선택한 항목(index)이 리스트박스 맨 위에 보이도록 스크롤 이동
                checkedListBox1.TopIndex = index;

                // 항목 선택 상태도 유지
                checkedListBox1.SelectedIndex = index;
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
    }
}
