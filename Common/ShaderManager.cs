using Common;
using Common.Abstractions;
using System;
using System.Collections.Generic;

namespace Common
{
    // 셰이더 매니저 구현
    public class ShaderManager
    {
        private static ShaderManager _instance;
        private static List<string> _compileMessages = new List<string>();
        private static bool _isPrintedCompileMessages = false;

        public static void AddCompileMessage(string message)
        {
            _compileMessages.Add(message);
        }

        public static void PrintCompileMessages()
        {
            if (_isPrintedCompileMessages) return;

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("{0, -10} | {1}", "유형", "셰이더 컴파일 메시지");
            Console.WriteLine(new string('-', 60));

            foreach (var msg in _compileMessages)
            {
                // 메시지 내용에 따라 접두사를 붙이거나 정렬할 수 있습니다.
                Console.WriteLine($"  LOG      | {msg}");
            }

            Console.WriteLine(new string('=', 60) + "\n");
            _isPrintedCompileMessages = true;
        }

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
            }
            else
            {
                ShaderManager.AddCompileMessage($"---> 셰이더 이미 존재: {shaderName}");
            }
        }

        public bool HasShader(string shaderName)
        {
            return _shaders.ContainsKey(shaderName);
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
