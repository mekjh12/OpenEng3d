using System.IO;

namespace ZetaExt
{
    /// <summary>
    /// 프로파일링 로그를 파일에 기록하는 유틸리티 클래스
    /// </summary>
    public static class LogProfile
    {
        // 로그 파일 경로를 저장하는 변수
        static string _fileName = "";

        // 파일에 쓰기 위한 StreamWriter 객체
        static StreamWriter sw = null;

        /// <summary>
        /// 지정된 파일명으로 로그 파일을 생성하고 StreamWriter를 초기화합니다.
        /// </summary>
        /// <param name="filename">생성할 로그 파일의 경로</param>
        public static void Create(string filename)
        {
            _fileName = filename;
            sw = new StreamWriter(filename);
        }

        /// <summary>
        /// 텍스트를 로그 파일에 씁니다. (줄바꿈 없음)
        /// </summary>
        /// <param name="txt">기록할 텍스트</param>
        public static void Write(string txt)
        {
            sw.Write(txt);
        }

        /// <summary>
        /// 텍스트를 로그 파일에 쓰고 줄바꿈을 추가합니다.
        /// </summary>
        /// <param name="txt">기록할 텍스트</param>
        public static void WriteLine(string txt)
        {
            sw.WriteLine(txt);
        }

        /// <summary>
        /// StreamWriter를 닫고 리소스를 해제합니다.
        /// </summary>
        public static void Dispose()
        {
            sw.Close();
            sw.Dispose();
        }
    }
}