using System;
using System.Collections.Generic;
using System.IO;

namespace ZetaExt
{
    /// <summary>
    /// 파일 해시를 관리하는 클래스
    /// </summary>
    public static class FileHashManager
    {
        private static Dictionary<string, string> _fileHashes;
        private static bool _isInitialized = false;
        private static string HASH_CACHE_FILE = "";
        public static string ROOT_FILE_PATH = "";

        /// <summary>
        /// 해시 관리자 초기화 및 저장된 해시 로드
        /// </summary>
        public static void Initialize(string filename)
        {
            if(string.IsNullOrEmpty(ROOT_FILE_PATH))
            {
                throw new Exception("ROOT_FILE_PATH가 설정되지 않았습니다.");
            }

            HASH_CACHE_FILE = ROOT_FILE_PATH + "\\FormTools\\bin\\Debug\\file_hashes.txt";
            if (!File.Exists(HASH_CACHE_FILE))
            {
                Console.WriteLine($"파일이 존재하지 않습니다: {HASH_CACHE_FILE}");
                return;
            }

            if (_isInitialized)
                return;

            if (_fileHashes == null)
            {
                _fileHashes = new Dictionary<string, string>();
                LoadHashes();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// 저장된 해시 정보 로드
        /// </summary>
        private static void LoadHashes()
        {
            Console.WriteLine("-----[세이더 파일 헤시]------");
            if (File.Exists(HASH_CACHE_FILE))
            {
                try
                {
                    string[] lines = File.ReadAllLines(HASH_CACHE_FILE);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length == 2)
                        {
                            _fileHashes[parts[0]] = parts[1];
                        }
                    }
                    Console.WriteLine($"파일 해시 정보 로드 완료: {_fileHashes.Count}개");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"파일 해시 정보 로드 오류: {ex.Message}");
                    _fileHashes.Clear();
                }
            }
        }

        /// <summary>
        /// 해시 정보 저장
        /// </summary>
        public static void SaveHashes()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> entry in _fileHashes)
                {
                    lines.Add($"{entry.Key}|{entry.Value}");
                }

                File.WriteAllLines(HASH_CACHE_FILE, lines);
                Console.WriteLine("파일 해시 정보 저장 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 해시 정보 저장 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일이 변경되었는지 확인
        /// </summary>
        /// <param name="filePath">확인할 파일 경로</param>
        /// <returns>파일이 변경되었거나 처음 확인하는 경우 true, 그렇지 않으면 false</returns>
        public static bool IsFileModified(string filePath)
        {
            if (!_isInitialized)
                Initialize(filePath);

            // 파일이 존재하지 않으면 변경되지 않은 것으로 간주
            if (!File.Exists(filePath))
                return false;

            // 현재 파일의 내용 해시 계산
            string currentHash = ComputeFileHash(filePath);

            if (_fileHashes == null)
            {
                _fileHashes = new Dictionary<string, string>();
                LoadHashes();
                _isInitialized = true;
            }

            // 이전에 계산된 해시가 없는 경우
            if (!_fileHashes.TryGetValue(filePath, out string savedHash))
            {
                _fileHashes[filePath] = currentHash;
                return true;
            }

            // 이전 해시와 현재 해시가 다른 경우
            if (currentHash != savedHash)
            {
                _fileHashes[filePath] = currentHash;
                return true;
            }

            // 파일 내용이 변경되지 않음
            return false;
        }

        /// <summary>
        /// 파일의 MD5 해시 계산
        /// </summary>
        /// <param name="filePath">해시를 계산할 파일 경로</param>
        /// <returns>파일 내용의 MD5 해시 문자열</returns>
        private static string ComputeFileHash(string filePath)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
    }
}
