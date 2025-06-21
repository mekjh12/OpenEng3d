using OpenGL;
using System;
using System.Collections.Generic;
using static Khronos.Platform;

namespace Common
{
    /// <summary>
    /// 모든 쉐이더에서 공통으로 사용할 유니폼 로드 기능을 제공하는 추상 클래스
    /// </summary>
    public abstract class UniformLoader<TEnum> where TEnum : Enum
    {
        protected Dictionary<string, int> _location = new Dictionary<string, int>();

        /// <summary>
        /// 개별 유니폼 변수의 위치를 가져오는 추상 메서드
        /// </summary>
        protected abstract int GetUniformLocation(string uniformName);

        // 기본 유니폼 로드 함수들
        public void LoadTexture(TEnum textureUniformName, TextureUnit textureUnit, uint texture)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            LoadInt(_location[textureUniformName.ToString()], ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadUniform(TEnum uniform, float value) => LoadFloat(_location[uniform.ToString()], value);
        public void LoadUniform(TEnum uniform, int value) => LoadInt(_location[uniform.ToString()], value);
        public void LoadUniform(TEnum uniform, bool value) => LoadBoolean(_location[uniform.ToString()], value);
        public void LoadUniform(TEnum uniform, Vertex4f vec) => LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(TEnum uniform, Vertex3f vec) => LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(TEnum uniform, Vertex2f vec) => LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(TEnum uniform, Vertex2i vec) => LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(TEnum uniform, Matrix4x4f mat) => LoadMatrix(_location[uniform.ToString()], mat);
        public void LoadUniform(TEnum uniform, Matrix3x3f mat) => LoadMatrix(_location[uniform.ToString()], mat);

        // ShaderProgram의 기존 메서드들은 protected virtual로 선언

        protected virtual void LoadFloat(int location, float value)
        {
            Gl.Uniform1f<float>(location, 1, value);
        }

        protected virtual void LoadInt(int location, int value)
        {
            Gl.Uniform1i<int>(location, 1, value);
        }

        protected virtual void LoadInt(int location, uint value)
        {
            Gl.Uniform1i<uint>(location, 1, value);
        }

        protected virtual void LoadVector(int location, Vertex3f value)
        {
            Gl.Uniform3f(location, 1, value);
        }
        protected virtual void LoadVector(int location, Vertex3i value)
        {
            Gl.Uniform3f(location, 1, value);
        }

        protected virtual void LoadVector(int location, Vertex2f value)
        {
            Gl.Uniform2f(location, 1, value);
        }

        protected virtual void LoadVector(int location, Vertex2i value)
        {
            Gl.Uniform2(location, new int[] { value.x, value.y });
        }

        protected virtual void LoadVector(int location, Vertex4f value)
        {
            Gl.Uniform4f(location, 1, value);
        }

        protected virtual void LoadBoolean(int location, bool value)
        {
            float toLoad = (value == true) ? 1.0f : 0.0f;
            Gl.Uniform1f(location, 1, toLoad);
        }

        protected virtual void LoadMatrix(int location, Matrix4x4f matrix)
        {
            Gl.UniformMatrix4(location, false, ((float[])matrix));
        }

        protected virtual void LoadMatrix(int location, Matrix3x3f matrix)
        {
            Gl.UniformMatrix3(location, false, ((float[])matrix));
        }
    }
}
