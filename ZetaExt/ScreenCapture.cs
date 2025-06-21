using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZetaExt
{
    public class ScreenCapture
    {
        //캡처 함수
        public static void Capture(int left, int top, int w, int h , int index)
        {
            Bitmap bitmap = new Bitmap(w, h);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Point point = new Point(left, top);
                g.CopyFromScreen(point, new Point(0, 0), new Size(w, h));
            }

            //저장 경로
            string path = Application.StartupPath + @"\" + index + ".png";

            //폴더 생성
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists == false)
            {
                dirInfo.Create();
            }

            //파일 이름
            //path += "\\" + DateTime.Now.ToString("yyMMdd_HHmmssfff") + ".png";
            bitmap.Save("C:\\Users\\mekjh\\OneDrive\\바탕 화면\\" + index + ".png");
        }
    }
}
