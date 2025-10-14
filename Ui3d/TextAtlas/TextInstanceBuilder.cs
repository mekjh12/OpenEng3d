using OpenGL;
using System;

namespace Ui3d
{
    /// <summary>
    /// 인스턴스 렌더링을 위한 글자별 데이터
    /// </summary>
    public struct CharInstanceData
    {
        // layout(location = 2) in vec3 aOffset;
        public float offsetX;
        public float offsetY;
        public float offsetZ;

        // layout(location = 3) in vec4 aUVRect;
        public float uvX;
        public float uvY;
        public float uvWidth;
        public float uvHeight;

        // layout(location = 4) in vec2 aCharSize;
        public float charWidth;
        public float charHeight;

        // 전체 크기 (sizeof 계산용)
        public const int SizeInBytes = 4 * (3 + 4 + 2); // 36 bytes
        public const int FloatCount = 9; // 9개의 float
    }

    /// <summary>
    /// 텍스트를 인스턴스 데이터로 변환하고 VBO를 관리하는 클래스
    /// </summary>
    public class TextInstanceBuilder : IDisposable
    {
        private uint _instanceVBO;
        private int _instanceBufferSize;
        private int _instanceCount;
        private float[] _dataBuffer;
        private const int INITIAL_BUFFER_SIZE = 128; // 최대 128글자 기본 할당

        /// <summary>
        /// 현재 인스턴스 개수 (글자 수)
        /// </summary>
        public int InstanceCount => _instanceCount;

        /// <summary>
        /// 인스턴스 VBO ID
        /// </summary>
        public uint InstanceVBO => _instanceVBO;

        public TextInstanceBuilder()
        {
            _instanceVBO = Gl.GenBuffer();
            _instanceBufferSize = 0;
            _instanceCount = 0;
            _dataBuffer = new float[INITIAL_BUFFER_SIZE * CharInstanceData.FloatCount];
        }

        /// <summary>
        /// 문자열로부터 인스턴스 데이터 생성
        /// </summary>
        /// <param name="text">렌더링할 텍스트</param>
        /// <param name="atlas">텍스처 아틀라스</param>
        /// <param name="startOffset">시작 오프셋 (선택적)</param>
        /// <returns>인스턴스 데이터 배열</returns>
        public CharInstanceData[] GenerateInstanceData(string text, CharacterTextureAtlas atlas, float startOffsetX = 0f, float startOffsetY = 0f, float startOffsetZ = 0f)
        {
            if (string.IsNullOrEmpty(text))
                return new CharInstanceData[0];

            CharInstanceData[] instances = new CharInstanceData[text.Length];

            float xOffset = startOffsetX;  // 현재 글자의 X 위치 (가로로 누적)

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // 아틀라스에서 글자 정보 가져오기
                CharInfo charInfo = atlas.GetCharInfo(c);

                // 위치 오프셋 설정
                instances[i].offsetX = xOffset;  // ✅ 이렇게만 하면 됩니다
                instances[i].offsetY = startOffsetY;
                instances[i].offsetZ = startOffsetZ;

                // UV 좌표 설정 (아틀라스 내 위치)
                instances[i].uvX = charInfo.uvX;
                instances[i].uvY = charInfo.uvY;
                instances[i].uvWidth = charInfo.uvWidth;
                instances[i].uvHeight = charInfo.uvHeight;

                // 글자 크기 설정 (월드 공간에서의 크기)
                instances[i].charWidth = charInfo.width;
                instances[i].charHeight = charInfo.height;

                // 다음 글자 위치로 이동 (자간 포함)
                xOffset += charInfo.advance;
            }

            return instances;
        }

        /// <summary>
        /// 중앙 정렬된 인스턴스 데이터 생성
        /// </summary>
        public CharInstanceData[] GenerateInstanceDataCentered(string text, CharacterTextureAtlas atlas,
            float centerY = 0f, float centerZ = 0f)
        {
            if (string.IsNullOrEmpty(text))
                return new CharInstanceData[0];

            // 전체 텍스트 너비 계산
            float totalWidth = atlas.CalculateTextWidth(text);

            // 시작 위치를 중앙 기준으로 계산
            float startX = -totalWidth / 2f;

            return GenerateInstanceData(text, atlas, startX, centerY, centerZ);
        }

        /// <summary>
        /// CharInstanceData 배열을 float 배열로 변환
        /// </summary>
        /// <param name="instances">인스턴스 데이터 배열</param>
        /// <returns>GPU 업로드용 float 배열</returns>
        public float[] ConvertToFloatArray(CharInstanceData[] instances)
        {
            int requiredSize = instances.Length * CharInstanceData.FloatCount;

            // 버퍼 크기 확인 및 재할당
            if (_dataBuffer.Length < requiredSize)
            {
                int newSize = Math.Max(requiredSize, _dataBuffer.Length * 2);
                _dataBuffer = new float[newSize];
            }

            int index = 0;
            for (int i = 0; i < instances.Length; i++)
            {
                // vec3 aOffset
                _dataBuffer[index++] = instances[i].offsetX;
                _dataBuffer[index++] = instances[i].offsetY;
                _dataBuffer[index++] = instances[i].offsetZ;

                // vec4 aUVRect
                _dataBuffer[index++] = instances[i].uvX;
                _dataBuffer[index++] = instances[i].uvY;
                _dataBuffer[index++] = instances[i].uvWidth;
                _dataBuffer[index++] = instances[i].uvHeight;

                // vec2 aCharSize
                _dataBuffer[index++] = instances[i].charWidth;
                _dataBuffer[index++] = instances[i].charHeight;
            }

            return _dataBuffer;
        }

        /// <summary>
        /// 인스턴스 VBO 업데이트
        /// </summary>
        /// <param name="text">렌더링할 텍스트</param>
        /// <param name="atlas">텍스처 아틀라스</param>
        /// <param name="centered">중앙 정렬 여부</param>
        public void UpdateInstanceBuffer(string text, CharacterTextureAtlas atlas, bool centered = false)
        {
            // 인스턴스 데이터 생성
            CharInstanceData[] instances = centered
                ? GenerateInstanceDataCentered(text, atlas)
                : GenerateInstanceData(text, atlas);

            _instanceCount = instances.Length;

            if (_instanceCount == 0)
                return;

            // float 배열로 변환
            float[] data = ConvertToFloatArray(instances);

            // VBO 바인딩 및 업데이트
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);

            // 기존 버퍼가 충분히 크면 SubData, 아니면 새로 할당
            int requiredSize = _instanceCount * CharInstanceData.FloatCount * sizeof(float);
            if (requiredSize > _instanceBufferSize)
            {
                Gl.BufferData(BufferTarget.ArrayBuffer,
                    (uint)requiredSize,
                    data,
                    BufferUsage.DynamicDraw);
                _instanceBufferSize = requiredSize;
            }
            else
            {
                Gl.BufferSubData(BufferTarget.ArrayBuffer,
                    IntPtr.Zero,
                    (uint)requiredSize,
                    data);
            }

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// 인스턴스 VBO를 VAO에 설정
        /// </summary>
        /// <param name="vao">설정할 VAO</param>
        public void SetupVAOAttributes(uint vao)
        {
            Gl.BindVertexArray(vao);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);

            int stride = CharInstanceData.FloatCount * sizeof(float);

            // layout(location = 2) in vec3 aOffset;
            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribType.Float, false, stride, IntPtr.Zero);
            Gl.VertexAttribDivisor(2, 1); // 인스턴스마다 변경

            // layout(location = 3) in vec4 aUVRect;
            Gl.EnableVertexAttribArray(3);
            Gl.VertexAttribPointer(3, 4, VertexAttribType.Float, false, stride, (IntPtr)(3 * sizeof(float)));
            Gl.VertexAttribDivisor(3, 1);

            // layout(location = 4) in vec2 aCharSize;
            Gl.EnableVertexAttribArray(4);
            Gl.VertexAttribPointer(4, 2, VertexAttribType.Float, false, stride, (IntPtr)(7 * sizeof(float)));
            Gl.VertexAttribDivisor(4, 1);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            if (_instanceVBO != 0)
            {
                Gl.DeleteBuffers(_instanceVBO);
                _instanceVBO = 0;
            }
            _instanceBufferSize = 0;
            _instanceCount = 0;
        }
    }
}