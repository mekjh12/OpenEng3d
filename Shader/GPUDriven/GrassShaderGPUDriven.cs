using Common;
using OpenGL;
using ZetaExt;

namespace Shader
{
    /// <summary>
    /// GPU-Driven 풀 렌더링
    /// </summary>
    public class GrassShaderGPUDriven : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\grass_gpu_driven.vert";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\grass_gpu_driven.frag";

        // Uniform 위치
        private int loc_cameraRight;
        private int loc_cameraUp;
        private int loc_grassWidth;
        private int loc_grassHeight;

        private int loc_grassTexture;
        private int loc_heightmap;
        private int loc_normalMap;

        private int loc_heightScale;
        private int loc_terrainWorldSize;

        private int loc_sunDirection;
        private int loc_grassColorTop;
        private int loc_grassColorBottom;

        // LOD 관련 (수정!)
        private int loc_currentLOD;       // 현재 렌더링 중인 LOD
        private int loc_grassPerTile;     // 해당 LOD의 풀 개수

        public GrassShaderGPUDriven(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            // 더미 VAO - 어트리뷰트 없음!
        }

        protected override void GetAllUniformLocations()
        {
            loc_cameraRight = GetUniformLocation("u_CameraRight");
            loc_cameraUp = GetUniformLocation("u_CameraUp");
            loc_grassWidth = GetUniformLocation("u_GrassWidth");
            loc_grassHeight = GetUniformLocation("u_GrassHeight");

            loc_grassTexture = GetUniformLocation("u_GrassTexture");
            loc_heightmap = GetUniformLocation("u_Heightmap");
            loc_normalMap = GetUniformLocation("u_NormalMap");

            loc_heightScale = GetUniformLocation("u_HeightScale");
            loc_terrainWorldSize = GetUniformLocation("u_TerrainWorldSize");

            loc_sunDirection = GetUniformLocation("u_SunDirection");
            loc_grassColorTop = GetUniformLocation("u_GrassColorTop");
            loc_grassColorBottom = GetUniformLocation("u_GrassColorBottom");

            // LOD 관련
            loc_currentLOD = GetUniformLocation("u_CurrentLOD");
            loc_grassPerTile = GetUniformLocation("u_GrassPerTile");
        }

        /// <summary>
        /// 현재 LOD 및 풀 개수 로드
        /// </summary>
        public void LoadCurrentLOD(int lod, int grassPerTile)
        {
            Gl.Uniform1(loc_currentLOD, lod);
            Gl.Uniform1(loc_grassPerTile, grassPerTile);
        }

        public void LoadCameraVectors(in Vertex3f right, in Vertex3f up)
        {
            Gl.Uniform3(loc_cameraRight, right.x, right.y, right.z);
            Gl.Uniform3(loc_cameraUp, up.x, up.y, up.z);
        }

        public void LoadGrassSize(float width, float height)
        {
            Gl.Uniform1(loc_grassWidth, width);
            Gl.Uniform1(loc_grassHeight, height);
        }

        public void LoadGrassTexture(uint textureID)
        {
            Gl.Uniform1(loc_grassTexture, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
        }

        public void LoadHeightmap(uint textureID)
        {
            Gl.Uniform1(loc_heightmap, 1);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
        }

        public void LoadNormalMap(uint textureID)
        {
            Gl.Uniform1(loc_normalMap, 2);
            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
        }

        public void LoadHeightScale(float scale)
        {
            Gl.Uniform1(loc_heightScale, scale);
        }

        public void LoadTerrainWorldSize(float width, float height)
        {
            Gl.Uniform2(loc_terrainWorldSize, width, height);
        }

        public void LoadSunDirection(in Vertex3f dir)
        {
            float len = dir.Length();
            float nx = dir.x / len;
            float ny = dir.y / len;
            float nz = dir.z / len;
            Gl.Uniform3(loc_sunDirection, nx, ny, nz);
        }

        public void LoadGrassColors(in Vertex3f top, in Vertex3f bottom)
        {
            Gl.Uniform3(loc_grassColorTop, top.x, top.y, top.z);
            Gl.Uniform3(loc_grassColorBottom, bottom.x, bottom.y, bottom.z);
        }
    }
}