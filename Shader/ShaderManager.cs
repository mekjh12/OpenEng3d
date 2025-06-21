using Common;
using Common.Abstractions;
using System;
using System.Collections.Generic;

namespace Shader
{
    // 셰이더 매니저 구현
    public class ShaderManager
    {
        private static ShaderManager _instance;

        public static ShaderManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ShaderManager();
                return _instance;
            }
        }

        // 인터페이스를 통해 모든 셰이더 타입 저장 가능
        private Dictionary<string, IShaderProgram> _shaders = new Dictionary<string, IShaderProgram>();

        // 셰이더 추가
        public void AddShader(IShaderProgram shader)
        {
            string shaderName = shader.Name;
            if (!_shaders.ContainsKey(shaderName))
            {
                _shaders[shaderName] = shader;
                Console.WriteLine($"* 셰이더 추가됨: {shaderName}");
            }
            else
            {
                Console.WriteLine($"! 셰이더 이미 존재: {shaderName}");
            }
        }

        // 셰이더 가져오기 (제네릭 사용)
        public T GetShader<T>(string shaderName = null) where T : class, IShaderProgram
        {
            if (string.IsNullOrEmpty(shaderName))
                shaderName = typeof(T).Name;

            if (_shaders.ContainsKey(shaderName))
            {
                return _shaders[shaderName] as T;
            }

            Console.WriteLine($"! 셰이더 찾을 수 없음: {shaderName}");
            return null;
        }

        // 모든 셰이더 해제
        public void CleanupAll()
        {
            foreach (var shader in _shaders.Values)
            {
                shader.CleanUp();
            }
            _shaders.Clear();
            Console.WriteLine("* 모든 셰이더 정리됨");
        }
    }
}
