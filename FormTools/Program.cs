using System;
using System.Windows.Forms;

namespace FormTools
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormTerrain());
            //Application.Run(new FormCloud());                     // 작동됨
            //Application.Run(new FormPhysics());                   // 작동됨
            //Application.Run(new FormAnimation());                 // 작동됨
            //Application.Run(new FormHzm());
            //Application.Run(new FormOcclusionQuery());
            //Application.Run(new FormTexture3d());
            //Application.Run(new FormAsyncTest());
            //Application.Run(new FormColor3Channel());
            //Application.Run(new FormTerrainImposter());
            //Application.Run(new FormImpostor());
            //Application.Run(new FormTileBaker());
            //Application.Run(new FormEntityTest());
            //Application.Run(new FormAtmosphereScattering());
            //Application.Run(new FormRealTimeCloudRendering());
            //Application.Run(new FormFrameBuffer());
            //Application.Run(new FormTest());
            //Application.Run(new FormNoise3d());
            //Application.Run(new FormTest());
        }
    }
}
