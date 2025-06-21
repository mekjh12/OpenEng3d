using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZetaExt
{
    public class Debug
    {
        public static List<RenderLine> RenderLines = new List<RenderLine>();
        public static List<RenderPont> RenderPoints = new List<RenderPont>();

        public static void WriteHeadLine(string title)
        {
            Console.WriteLine(string.Concat(Enumerable.Repeat("=", 50)) + $" {title} " + string.Concat(Enumerable.Repeat("=", 50)));
        }

        public static void AddPoint(Vertex3f pos, Vertex4f color, float thick)
        {
            RenderPoints.Add(new RenderPont(pos, color, thick));
        }

        public static void AddVector(Vertex3f pos, Vertex3f normal, Vertex4f color, float thick) 
        {
            RenderLines.Add(new RenderLine(pos, pos + normal, color, thick));
        }

        static string _txt = "";
        static string _eachFrameText = "";

        public static string TextFrame => _eachFrameText;

        public static string Text => _txt;

        /// <summary>
        /// 프레임에 상관없이 호출시 지운다.
        /// </summary>
        public static void Clear()
        {
            _txt = "";
        }

        /// <summary>
        /// 디버깅을 위한 점과 선을 모두 지운다.
        /// </summary>
        public static void ClearPointAndLine()
        {
            RenderLines.Clear();
            RenderPoints.Clear();
        }

        /// <summary>
        /// 매 프레임마다 지운다.
        /// </summary>
        public static void ClearFrameText()
        {
            _eachFrameText = "";
        }

        /// <summary>
        /// 프레임마다 쓴다.
        /// </summary>
        /// <param name="text"></param>
        public static void Write(string text)
        {
            _eachFrameText += text;
        }

        public static void WriteLine(string text)
        {
            _eachFrameText += text + " <br> ";
        }

        /// <summary>
        /// 프로그램이 종료될때까지 누적된 디버깅이다.
        /// 프레임율을 떨어지게하므로 디버깅에서만 제한적으로 사용하세요.
        /// </summary>
        /// <param name="text"></param>
        public static void PrintLine(string text)
        {
            _txt = text + " <br> " +  _txt;
        }

        /// <summary>
        /// 프로그램이 종료될때까지 누적된 디버깅이다.
        /// </summary>
        /// <param name="text"></param>
        public static void Print(string text)
        {
            _txt = text + _txt;
        }
    }
}
