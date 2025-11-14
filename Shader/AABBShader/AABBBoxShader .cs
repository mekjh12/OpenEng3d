using Common;
using OpenGL;
using System;

namespace Shader
{
    public class AABBBoxShader : ShaderProgram<AABBBoxShader.UNIFORM_NAME>
    {
        const string VERTEX_FILE = @"\Shader\AABBShader\aabb.vert";
        const string GEOMETRY_FILE = @"\Shader\AABBShader\aabb.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\AABBShader\aabb.frag";

        public enum UNIFORM_NAME
        {
            vp,
            Count
        }

        // 인스턴스 VBO
        private uint _instanceCenterBuffer;
        private uint _instanceHalfSizeBuffer;
        private uint _instanceColorBuffer;

        private const int MAX_INSTANCES = 10000;
            
        // 재사용 버퍼
        private float[] _centerData; 
        private float[] _halfSizeData;
        private float[] _colorData;

        public AABBBoxShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();

            // 데이터 버퍼 초기화
            _centerData = new float[MAX_INSTANCES * 3];    // ⭐ 추가
            _halfSizeData = new float[MAX_INSTANCES * 3];  // vec3 = 3 floats
            _colorData = new float[MAX_INSTANCES * 4];     // vec4 = 4 floats

            // VBO 생성
            _instanceCenterBuffer = Gl.GenBuffer();        // ⭐ 추가
            _instanceHalfSizeBuffer = Gl.GenBuffer();
            _instanceColorBuffer = Gl.GenBuffer();

            // ⭐ Center 버퍼 설정
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceCenterBuffer);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(MAX_INSTANCES * 3 * sizeof(float)),
                IntPtr.Zero,
                BufferUsage.DynamicDraw);

            // HalfSize 버퍼 설정
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceHalfSizeBuffer);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(MAX_INSTANCES * 3 * sizeof(float)),
                IntPtr.Zero,
                BufferUsage.DynamicDraw);

            // Color 버퍼 설정
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceColorBuffer);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(MAX_INSTANCES * 4 * sizeof(float)),
                IntPtr.Zero,
                BufferUsage.DynamicDraw);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "instanceHalfSize");
            base.BindAttribute(2, "instanceColor");
        }

        /// <summary>
        /// 인스턴스 데이터 업로드
        /// </summary>
        public unsafe void UploadInstanceData(Vertex3f[] centers, Vertex3f[] halfSizes, Vertex4f[] colors, int count)
        {
            if (count > MAX_INSTANCES)
            {
                count = MAX_INSTANCES;
            }

            // ⭐ Center 데이터 복사
            for (int i = 0; i < count; i++)
            {
                int offset = i * 3;
                _centerData[offset + 0] = centers[i].x;
                _centerData[offset + 1] = centers[i].y;
                _centerData[offset + 2] = centers[i].z;
            }

            // HalfSize 데이터 복사
            for (int i = 0; i < count; i++)
            {
                int offset = i * 3;
                _halfSizeData[offset + 0] = halfSizes[i].x;
                _halfSizeData[offset + 1] = halfSizes[i].y;
                _halfSizeData[offset + 2] = halfSizes[i].z;
            }

            // Color 데이터 복사
            for (int i = 0; i < count; i++)
            {
                int offset = i * 4;
                _colorData[offset + 0] = colors[i].x;
                _colorData[offset + 1] = colors[i].y;
                _colorData[offset + 2] = colors[i].z;
                _colorData[offset + 3] = colors[i].w;
            }

            // ⭐ GPU에 Center 업로드
            fixed (float* centerPtr = _centerData)
            {
                Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceCenterBuffer);
                Gl.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero,
                    (uint)(count * 3 * sizeof(float)),
                    new IntPtr(centerPtr));
            }

            // GPU에 HalfSize 업로드
            fixed (float* halfSizePtr = _halfSizeData)
            {
                Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceHalfSizeBuffer);
                Gl.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero,
                    (uint)(count * 3 * sizeof(float)),
                    new IntPtr(halfSizePtr));
            }

            // GPU에 Color 업로드
            fixed (float* colorPtr = _colorData)
            {
                Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceColorBuffer);
                Gl.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero,
                    (uint)(count * 4 * sizeof(float)),
                    new IntPtr(colorPtr));
            }

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// VAO에 인스턴스 속성 설정
        /// </summary>
        public void SetupInstancedAttributes(uint vao)
        {
            Gl.BindVertexArray(vao);

            // ⭐ Center 속성 (location 0)
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceCenterBuffer);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 0, IntPtr.Zero);
            Gl.VertexAttribDivisor(0, 1);  // 인스턴스마다 1번씩 증가

            // HalfSize 속성 (location 1)
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceHalfSizeBuffer);
            Gl.EnableVertexAttribArray(1);
            Gl.VertexAttribPointer(1, 3, VertexAttribType.Float, false, 0, IntPtr.Zero);
            Gl.VertexAttribDivisor(1, 1);

            // Color 속성 (location 2)
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceColorBuffer);
            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 4, VertexAttribType.Float, false, 0, IntPtr.Zero);
            Gl.VertexAttribDivisor(2, 1);

            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }


    }
}