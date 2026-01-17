using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ZetaExt
{
    /// <summary>
    /// 파일 해시를 관리하는 클래스
    /// </summary>
    public static class FileHashManager
    {
        // 원본 로드 데이터 (읽기 전용, 백업용)
        private static Dictionary<string, string> _fileHashesBackground = new Dictionary<string, string>();

        // 현재 세션에서 체크한 파일들의 현재 해시
        private static Dictionary<string, string> _fileHashes = new Dictionary<string, string>();

        private static bool _isInitialized = false;
        private static bool _hasChanges = false;
        private static string HASH_CACHE_FILE = "";
        public static string ROOT_FILE_PATH = "";

        /// <summary>
        /// 해시 관리자 초기화 및 저장된 해시 로드
        /// </summary>
        public static void Initialize()
        {
            if (string.IsNullOrEmpty(ROOT_FILE_PATH))
            {
                throw new Exception("ROOT_FILE_PATH가 설정되지 않았습니다.");
            }

            if (_isInitialized)
                return;

            HASH_CACHE_FILE = Path.Combine(ROOT_FILE_PATH, "FormTools", "bin", "Debug", "file_hashes.txt");

            // 디렉토리가 없으면 생성
            string directory = Path.GetDirectoryName(HASH_CACHE_FILE);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"디렉토리 생성: {directory}");
            }

            LoadHashes();
            _isInitialized = true;
            _hasChanges = false;
        }

        /// <summary>
        /// 저장된 해시 정보 로드
        /// </summary>
        private static void LoadHashes()
        {
            Console.WriteLine("-------------------[파일 해시 로드]------------------------");

            _fileHashesBackground.Clear();
            _fileHashes.Clear();

            if (!File.Exists(HASH_CACHE_FILE))
            {
                Console.WriteLine($"해시 파일이 존재하지 않습니다. 새로 생성됩니다: {HASH_CACHE_FILE}");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(HASH_CACHE_FILE);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split('|');
                    if (parts.Length == 2)
                    {
                        // Background에 원본 데이터 저장
                        _fileHashesBackground[parts[0]] = parts[1];
                    }
                }
                Console.WriteLine($"파일 해시 정보 로드 완료: {_fileHashesBackground.Count}개");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 해시 정보 로드 오류: {ex.Message}");
                _fileHashesBackground.Clear();
            }
        }

        /// <summary>
        /// 파일이 변경되었는지 확인 (메모리에만 저장, 실제 파일 저장은 안 함)
        /// </summary>
        /// <param name="filePath">확인할 파일 경로</param>
        /// <returns>파일이 변경되었거나 처음 확인하는 경우 true</returns>
        public static bool IsFileModified(string filePath)
        {
            if (!_isInitialized)
                Initialize();

            // 파일이 존재하지 않으면 변경되지 않은 것으로 간주
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"파일이 존재하지 않음: {filePath}");
                return false;
            }

            // 절대 경로로 변환
            string absolutePath = Path.GetFullPath(filePath);

            // 현재 파일의 수정 시간 해시 계산
            string currentHash = ComputeFileTimeHash(absolutePath);

            // Background에서 이전 해시 가져오기 (없으면 null)
            _fileHashesBackground.TryGetValue(absolutePath, out string savedHash);

            // 현재 세션에서 체크한 파일로 등록 (변경 여부와 관계없이)
            _fileHashes[absolutePath] = currentHash;

            // 이전 해시가 없는 경우 (새 파일)
            if (savedHash == null)
            {
                Console.WriteLine($"[새 파일 감지] {Path.GetFileName(absolutePath)}");
                _hasChanges = true;
                return true;
            }

            // 이전 해시와 현재 해시가 다른 경우 (파일 변경됨)
            if (currentHash != savedHash)
            {
                Console.WriteLine($"[파일 변경 감지] {Path.GetFileName(absolutePath)}");
                Console.WriteLine($"  이전: {savedHash}");
                Console.WriteLine($"  현재: {currentHash}");
                _hasChanges = true;
                return true;
            }

            // 파일 내용이 변경되지 않음
            return false;
        }

        /// <summary>
        /// 모든 변경사항을 파일에 저장하고 종료
        /// 프로그램 종료 시 반드시 호출해야 함
        /// </summary>
        public static void Finalize()
        {
            if (!_isInitialized)
                return;

            Console.WriteLine("-------------------[파일 해시 저장]------------------------");

            if (_hasChanges || _fileHashes.Count > 0)
            {
                // Background를 기반으로 시작
                Dictionary<string, string> finalHashes = new Dictionary<string, string>(_fileHashesBackground);

                // 현재 세션에서 체크한 파일들로 업데이트
                foreach (var entry in _fileHashes)
                {
                    finalHashes[entry.Key] = entry.Value;
                }

                SaveHashes(finalHashes);
                Console.WriteLine($"체크한 파일: {_fileHashes.Count}개");
                Console.WriteLine($"전체 저장: {finalHashes.Count}개");
            }
            else
            {
                Console.WriteLine("변경사항 없음, 저장 생략");
            }

            _hasChanges = false;
        }

        /// <summary>
        /// 해시 정보를 파일에 저장 (내부용)
        /// </summary>
        private static void SaveHashes(Dictionary<string, string> hashes)
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> entry in hashes)
                {
                    lines.Add($"{entry.Key}|{entry.Value}");
                }

                File.WriteAllLines(HASH_CACHE_FILE, lines);
                Console.WriteLine($"파일 해시 정보 저장 완료: {hashes.Count}개");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 해시 정보 저장 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일의 마지막 수정 시간을 Ticks로 변환하여 해시 생성
        /// </summary>
        private static string ComputeFileTimeHash(string filePath)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
            long ticks = lastWriteTime.Ticks;

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = BitConverter.GetBytes(ticks);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
        }

        /// <summary>
        /// 특정 파일의 해시 강제 갱신 (메모리만)
        /// </summary>
        public static void UpdateFileHash(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            string absolutePath = Path.GetFullPath(filePath);
            string currentHash = ComputeFileTimeHash(absolutePath);
            _fileHashes[absolutePath] = currentHash;
            _hasChanges = true;
            Console.WriteLine($"파일 해시 강제 갱신: {Path.GetFileName(absolutePath)}");
        }

        /// <summary>
        /// 모든 해시 정보 초기화
        /// </summary>
        public static void ClearAllHashes()
        {
            _fileHashesBackground.Clear();
            _fileHashes.Clear();
            _hasChanges = true;
            Console.WriteLine("모든 해시 정보 초기화 (메모리)");
        }

        /// <summary>
        /// 현재까지 추적 중인 파일 개수 반환
        /// </summary>
        public static int GetTrackedFileCount()
        {
            return _fileHashesBackground.Count;
        }

        /// <summary>
        /// 현재 세션에서 체크한 파일 개수 반환
        /// </summary>
        public static int GetCheckedFileCount()
        {
            return _fileHashes.Count;
        }

        /// <summary>
        /// 변경사항이 있는지 확인
        /// </summary>
        public static bool HasChanges()
        {
            return _hasChanges;
        }

        /// <summary>
        /// 디버그용: 현재 상태 출력
        /// </summary>
        public static void PrintStatus()
        {
            Console.WriteLine("=== FileHashManager 상태 ===");
            Console.WriteLine($"Background 파일 수: {_fileHashesBackground.Count}");
            Console.WriteLine($"체크한 파일 수: {_fileHashes.Count}");
            Console.WriteLine($"변경사항: {(_hasChanges ? "있음" : "없음")}");
        }
    }
}