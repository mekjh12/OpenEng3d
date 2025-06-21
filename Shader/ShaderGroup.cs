using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Shader
{

    public class ShaderGroup
    {
        Dictionary<int, ShaderProgram<Enum>> _list;

        StaticShader _sShader;
        ColorShader _cShader;
        SkyBoxShader _skyShader;
        AnimateShader _ashader;
        BillboardShader _billShader;
        InertiaShader _inertiaShader;
        VolumeRenderShader _volumeRenderShader;
        WorleyNoiseShader _worleyNoiseShader;
        WorleyNoiseGenShader _worleyNoiseGenShader;
        InfiniteGridShader _infiniteGridShader;
        NoiseShader _noiseShader;
        ScreenShader _screenShader;

        bool _isStaticShader = false;
        bool _isColorShader = false;
        bool _isSkyBoxShader = false;
        bool _isAnimateShader = false;
        bool _isBillboardShader = false;
        bool _isInertiaShader = false;
        bool _isVolumeShader = false;
        bool _isWorleyNoiseShader = false;
        bool _isWorleyNoiseGenShader = false;
        bool _isInfiniteGridShader = false;
        bool _isNoiseShader = false;
        bool _isScreenShader = false;

        public bool IsScreenShader
        {
            get => _isScreenShader;
            set => _isScreenShader = value;
        }

        public bool IsNoiseShader
        {
            get => _isNoiseShader;
            set => _isNoiseShader = value;
        }

        public bool IsInfiniteGridShader
        {
            get => _isInfiniteGridShader;
            set => _isInfiniteGridShader = value;
        }

        public bool IsWorleyNoiseGenShader
        {
            get => _isWorleyNoiseGenShader;
            set => _isWorleyNoiseGenShader = value;
        }

        public bool IsWorleyNoiseShader
        {
            get => _isWorleyNoiseShader;
            set => _isWorleyNoiseShader = value;
        }

        public bool IsInertialShader
        {
            get => _isInertiaShader;
            set => _isInertiaShader = value;
        }

        public bool IsStaticShader
        {
            get => _isStaticShader;
            set => _isStaticShader = value;
        }

        public bool IsSkyBoxShader
        {
            get => _isSkyBoxShader;
            set => _isSkyBoxShader = value;
        }

        public bool IsColorShader
        {
            get => _isColorShader; 
            set => _isColorShader = value;
        }

        public bool IsAnimateShader
        {
            get => _isAnimateShader; 
            set => _isAnimateShader = value;
        }

        public bool IsBillboardShader
        {
            get => _isBillboardShader; 
            set => _isBillboardShader = value;
        }

        public bool IsVolumeRenderShader
        {
            get => _isVolumeShader;
            set => _isVolumeShader = value;
        }

        public NoiseShader NoiseShader => _noiseShader;

        public InfiniteGridShader InfiniteGridShader => _infiniteGridShader;

        public WorleyNoiseGenShader WorleyNoiseGenShader => _worleyNoiseGenShader;

        public WorleyNoiseShader WorleyNoiseShader => _worleyNoiseShader;

        public StaticShader StaticShader => _sShader;

        public ColorShader ColorShader => _cShader;

        public SkyBoxShader SkyBoxShader => _skyShader;

        public AnimateShader AnimateShader => _ashader;

        public BillboardShader BillboardShader => _billShader;

        public InertiaShader InertiaShader => _inertiaShader;

        public VolumeRenderShader VolumeRenderShader => _volumeRenderShader;

        public ScreenShader ScreenShader => _screenShader;

        public ShaderGroup()
        {
            _list = new Dictionary<int, ShaderProgram<Enum>>();
            ZetaExt.Debug.WriteHeadLine("쉐이더");
        }

        public void Create(string path)
        {
            if (_isStaticShader) _sShader = new StaticShader(path);
            if (_isColorShader) _cShader = new ColorShader(path);
            if (_isSkyBoxShader) _skyShader = new SkyBoxShader(path);
            if (_isAnimateShader) _ashader = new AnimateShader(path);
            if (_isBillboardShader) _billShader = new BillboardShader(path);
            if (_isInertiaShader) _inertiaShader = new InertiaShader(path);
            if (_isVolumeShader) _volumeRenderShader = new VolumeRenderShader(path);
            if (_isWorleyNoiseShader) _worleyNoiseShader = new WorleyNoiseShader(path);
            if (_isWorleyNoiseGenShader) _worleyNoiseGenShader = new WorleyNoiseGenShader(path);
            if (_isInfiniteGridShader) _infiniteGridShader = new InfiniteGridShader(path);
            if (_isNoiseShader) _noiseShader = new NoiseShader(path);
            if (_isScreenShader) _screenShader = new ScreenShader(path);
        }
    }
}
