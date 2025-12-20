using OpenGL;
using System;
using System.Collections.Generic;

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
        private CharInstanceData[] _instances;
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

        private float[] _floatBuffer = new float[512 * CharInstanceData.FloatCount];  // 512글자 * 8 floats


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
        public void ConvertToFloatArray(CharInstanceData[] instances, int count, ref float[] targetBuffer)
        { 
            for (int i = 0; i < count; i++)
            {
                int offset = i * CharInstanceData.FloatCount;  // i * 9
                targetBuffer[offset + 0] = instances[i].offsetX;
                targetBuffer[offset + 1] = instances[i].offsetY;
                targetBuffer[offset + 2] = instances[i].offsetZ;
                targetBuffer[offset + 3] = instances[i].uvX;
                targetBuffer[offset + 4] = instances[i].uvY;
                targetBuffer[offset + 5] = instances[i].uvWidth;
                targetBuffer[offset + 6] = instances[i].uvHeight;
                targetBuffer[offset + 7] = instances[i].charWidth;
                targetBuffer[offset + 8] = instances[i].charHeight;
            }
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
            _instances = centered
                ? GenerateInstanceDataCentered(text, atlas)
                : GenerateInstanceData(text, atlas);

            _instanceCount = _instances.Length;

            if (_instanceCount == 0)
                return;

            // float 배열로 변환
            ConvertToFloatArray(_instances, _instanceCount, ref _floatBuffer);

            // VBO 바인딩 및 업데이트
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);

            // 기존 버퍼가 충분히 크면 SubData, 아니면 새로 할당
            int requiredSize = _instanceCount * CharInstanceData.FloatCount * sizeof(float);
            if (requiredSize > _instanceBufferSize)
            {
                Gl.BufferData(BufferTarget.ArrayBuffer,
                    (uint)requiredSize,
                    _floatBuffer,
                    BufferUsage.DynamicDraw);
                _instanceBufferSize = requiredSize;
            }
            else
            {
                Gl.BufferSubData(BufferTarget.ArrayBuffer,
                    IntPtr.Zero,
                    (uint)requiredSize,
                    _floatBuffer);
            }

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// 인스턴스 개수를 직접 설정 (외부에서 데이터를 직접 관리할 때 사용)
        /// </summary>
        public void SetInstanceCount(int count)
        {
            _instanceCount = count;
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


        // 기존 리스트에 직접 추가 (배열 할당 없음)
        public void GenerateInstanceDataInto(
            string text,
            int startIndex,
            int length,
            CharacterTextureAtlas atlas,
            float startX, float startY, float z,
            List<CharInstanceData> output)
        {
            float currentX = startX;

            for (int i = 0; i < length; i++)
            {
                char c = text[startIndex + i];
                CharInfo charInfo = atlas.GetCharInfo(c);

                // GetCharInfo는 항상 유효한 값을 반환 (기본값 포함)
                if (charInfo.character == '\0') continue; // 기본값 체크

                output.Add(new CharInstanceData
                {
                    offsetX = currentX,
                    offsetY = startY,
                    offsetZ = z,
                    charWidth = charInfo.width,
                    charHeight = charInfo.height,
                    uvX = charInfo.uvX,
                    uvY = charInfo.uvY,
                    uvWidth = charInfo.uvWidth,
                    uvHeight = charInfo.uvHeight
                });

                currentX += charInfo.width;
            }
        }

        // List를 배열로 변환하지 않고 직접 float 배열에 쓰기
        public void ConvertToFloatArrayInto(List<CharInstanceData> instances, float[] output)
        {
            int floatIndex = 0;
            for (int i = 0; i < instances.Count; i++)
            {
                var inst = instances[i];
                output[floatIndex++] = inst.offsetX;
                output[floatIndex++] = inst.offsetY;
                output[floatIndex++] = inst.offsetZ;
                output[floatIndex++] = inst.charWidth;
                output[floatIndex++] = inst.charHeight;
                output[floatIndex++] = inst.uvX;
                output[floatIndex++] = inst.uvY;
                output[floatIndex++] = inst.uvWidth;
                output[floatIndex++] = inst.uvHeight;
            }
        }
    }
}