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
            //Application.Run(new FormTerrainDataTest());           // 작동됨(수정 필요)
            //Application.Run(new FormOcclusionOpt());              // 작동됨(실패)
            //Application.Run(new FormGPUImposterInstance());       // 작동됨(실패)
            //Application.Run(new FormBillboardCloud());            // 작동됨            
            //Application.Run(new FormGPUDriveHiZ());                 // 작동됨(실패)
            //Application.Run(new FormGPUDriven());                 // 작동됨(이전버전으로 폐기)
            Application.Run(new FormGPUDrivenFrustumHiZ());           // 작동됨 = 뷰컬링+HiZ컬링(LOD0 모델원형인스턴스, LOD1 AABB인스턴스)
            //Application.Run(new FormGPUDrivenImposter());           // 작동됨 = 뷰컬링(LOD0 모델원형인스턴스, LOD1 AABB인스턴스)
            //Application.Run(new FormGPUDrivenModelInstance());      // 작동됨 = 모델 원형 인스턴스렌더링(카메라 근방)
            //Application.Run(new FormImpostor());                  // 작동됨
            //Application.Run(new FormUnifiedModel());              // 작동됨
            //Application.Run(new FormQuadTree());                  // 작동됨
            //Application.Run(new FormCulling());                   // 작동됨
            //Application.Run(new FormHZBuffer());                  // 작동됨
            //Application.Run(new FormBVH());                       // 작동됨
            //Application.Run(new FormCloud());                     // 작동됨
            //Application.Run(new FormPhysics());                   // 작동됨
            //Application.Run(new FormAnimation());                 // 작동됨
            //Application.Run(new FormHzm());                       // 작동됨
            //Application.Run(new FormTerrain());
            //Application.Run(new FormOcclusionQuery());
            //Application.Run(new FormTexture3d());
            //Application.Run(new FormAsyncTest());
            //Application.Run(new FormColor3Channel());
            //Application.Run(new FormTerrainImposter());
            //Application.Run(new FormTileBaker());
            //Application.Run(new FormEntityTest());
            //Application.Run(new FormAtmosphereScattering());
            //Application.Run(new FormRealTimeCloudRendering());
            //Application.Run(new FormFrameBuffer());
            //Application.Run(new FormTest());
            //Application.Run(new FormNoise3d());
            //Application.Run(new FormTest());
            //Application.Run(new FormGPUDrivenQuadTree());           // 작동됨(실패)

        }
    }
}
