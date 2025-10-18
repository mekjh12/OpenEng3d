using Animate;
using AutoGenEnums;
using Common.Abstractions;
using GlWindow;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormAnimation : Form
    {

        private readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";

        private GlControl3 _glControl3;

        StaticShader _staticShader;
        AnimateShader _animateShader;
        AxisShader _axisShader;
        ColorShader _colorShader;

        MixamoRotMotionStorage _mixamoRotMotionStorage;
        MixamoRotMotionStorage _mixamoRotMotionStorageB;
        List<IAnimActor> _aniActors = new List<IAnimActor>();
        private int _lastGen0Count = 0;
        private int _tick = 0;

        Bitmap _itemBitmap = null;

        bool _isLeftTwoBoneIK = true;
        SingleBoneLookAt _singleLookAt;
        SingleBoneLookAt _singleLookAt2;

        string testBone = MIXAMORIG_BONENAME.mixamorig_Head;
        string testBone2 = MIXAMORIG_BONENAME.mixamorig_LeftArm;
        LocalSpaceAxis testForward = LocalSpaceAxis.Y;
        LocalSpaceAxis testUp = LocalSpaceAxis.Z;

        public FormAnimation()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("characterAnimation", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
                BackClearColor = new Vertex3f(0, 0, 0),
                IsVisibleUi2d = true,
            };

            _glControl3.Init += (w, h) => Init(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = UpdateFrame;
            _glControl3.RenderFrame = RenderFrame;
            _glControl3.MouseDown += (s, e) => MouseDnEvent(s, e);
            _glControl3.MouseUp += (s, e) => MouseUpEvent(s, e);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);

            _glControl3.Start();
            _glControl3.SetVisibleMouse(true);
            Controls.Add(_glControl3);

            // 파일 해시 초기화
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;
        }

        private void FormUi2d_Load(object sender, EventArgs e)
        {
            MemoryProfiler.StartFrameMonitoring();
        }

        public void Init(int width, int height)
        {
            // 랜덤변수 생성
            Rand.InitSeed(500);

            // 셰이더 초기화
            if (_staticShader == null) _staticShader = new StaticShader(PROJECT_PATH);
            if (_animateShader == null) _animateShader = new AnimateShader(PROJECT_PATH);
            if (_axisShader == null) _axisShader = new AxisShader(PROJECT_PATH);
            if (_colorShader == null) _colorShader = new ColorShader(PROJECT_PATH);

            // ✅ 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        private void Init2d(int w, int h)
        {
            // 화면 구성요소 초기화
            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("resolution", $"resolution={w >> 2}x{h >> 2}", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));
        }

        private void Init3d(int w, int h)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // 1. 아틀라스 초기화
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            //CharacterTextureAtlas.Instance.SaveAtlasToFile(PROJECT_PATH + @"\Res\Ui2d\CharacterTextureAtlas.png");

            // 애니메이션 모델 및 모션 로드
            _mixamoRotMotionStorage = new MixamoRotMotionStorage();

            // [캐릭터] =========================================
            const string HUMAN_HIP_BONENAME = "mixamorig_Hips";
            PrimateRig aniRig = new PrimateRig(PROJECT_PATH + @"\Res\Actor\abe\abe.dae", HUMAN_HIP_BONENAME, isLoadAnimation: false);
            //PrimateRig aniRig1 = new PrimateRig(PROJECT_PATH + @"\Res\Actor\Hero\aa_heroNasty.dae", HUMAN_HIP_BONENAME, isLoadAnimation: false);
            //PrimateRig aniRig2 = new PrimateRig(PROJECT_PATH + @"\Res\Actor\Guybrush\Guybrush.dae", HUMAN_HIP_BONENAME, isLoadAnimation: false);

            int px = -30;
            int py = -30;

            for (int i=0; i<100; i++)
            {
                break;
                Human human = new Human($"abe_{i}", aniRig);
                px += 3;
                human.Transform.IncreasePosition(px, py, 0);
                _aniActors.Add(human);
                if (i % 10 == 0)
                {
                    px = 0;
                    py += 3;
                }
            }

            _aniActors.Add(new Human("abe1abe1아베abe1abe1아베abe1abe1아베abe1abe1아베abe1abe1아베abe1abe1아베", aniRig));
            _aniActors[0].Transform.IncreasePosition(1, 1, 0);
            if (_aniActors[0] is Human)
            {
                Human human = _aniActors[0] as Human;
                human.AddBoneTagNamePlate(_glControl3.Camera, "상완", MIXAMORIG_BONENAME.mixamorig_LeftArm);
                human.AddBoneTagNamePlate(_glControl3.Camera, "어깨", MIXAMORIG_BONENAME.mixamorig_LeftShoulder);
            }

            _itemBitmap = Bitmap.FromFile(PROJECT_PATH + @"\Res\Items\Item_Ingot_Gold.png") as Bitmap;

            //_aniActors.Add(new Human($"Guybrush", aniRig2));
            //_aniActors[1].Transform.IncreasePosition(4, 0.0f, 0);

            //_aniActors.Add(new Human($"aa_heroNasty", aniRig1));
            //_aniActors[2].Transform.IncreasePosition(6, 0.0f, 0);

            // 믹사모 애니메이션 로드
            _mixamoRotMotionStorage.Clear();
            foreach (string fileName in Directory.GetFiles(PROJECT_PATH + "\\Res\\Action\\Human\\"))
            {
                if (Path.GetExtension(fileName).Equals(".dae"))
                {
                    Motionable motion = MotionLoader.LoadMixamoMotion(aniRig, fileName);
                    _mixamoRotMotionStorage.AddMotion(motion);
                }
            }

            // 애니메이션 리타겟팅
            _mixamoRotMotionStorage.TransferRetargetMotions(targetAniRig: aniRig, sourceAniRig: aniRig);
            //_mixamoRotMotionStorage.TransferRetargetMotions(targetAniRig: aniRig2, sourceAniRig: aniRig);
            //_mixamoRotMotionStorage.TransferRetargetMotions(targetAniRig: aniRig1, sourceAniRig: aniRig);

            // 모션 블렌딩 예제
            //aniRig.AddBlendMotion("Jump-Defeated", "Jump", "Defeated", 1.0f, 2.0f, 0.8f);

            // 모션 레이어링 예제
            //LayeredMotion layerBlendMotion = new LayeredMotion("layerWalking", aniRig.GetMotion("Capoeira"));
            //layerBlendMotion.AddLayer(MixamoBone.Spine1, aniRig.GetMotion("a-T-Pose"));
            //layerBlendMotion.AddLayer(MixamoBone.LeftShoulder, aniRig.GetMotion("Defeated"));
            //layerBlendMotion.AddLayer(MixamoBone.Neck, aniRig.GetMotion("Jump"));
            //layerBlendMotion.AddLayer(MixamoBone.RightUpLeg, aniRig.GetMotion("Dying"));
            //layerBlendMotion.BuildTraverseBoneNamesCache(aniRig.Armature.RootBone);
            //layerBlendMotion.SetPeriodTime(0.5f);
            //aniRig.AddMotion(layerBlendMotion);

            // [당나귀] =========================================
            /*
            var donkeyRig = new DonkeyRig(PROJECT_PATH + @"\Res\Actor\Donkey\donkey.dae", hipBoneName: "CG", isLoadAnimation: false);
            _mixamoRotMotionStorageB = new MixamoRotMotionStorage();
            _mixamoRotMotionStorageB.Clear();
            foreach (string fileName in Directory.GetFiles(PROJECT_PATH + "\\Res\\Action\\Donkey\\"))
            {
                if (Path.GetExtension(fileName).Equals(".dae"))
                {
                    Motion motion = MotionLoader.LoadMixamoMotion(donkeyRig, fileName);
                    _mixamoRotMotionStorageB.AddMotion(motion);
                }
            }

            Donkey donkey = new Donkey($"donkey", donkeyRig);
            donkey.Transform.SetPosition(-2, 0, 0);
            _aniActors.Add(donkey);

            _mixamoRotMotionStorageB.TransferRetargetMotions(targetAniRig: donkeyRig, sourceAniRig: donkeyRig);
            */

            // -------------------------------------------------
            // 이종간 모션 링킹 생성
            //ArmatureLinker armatureLinker = new ArmatureLinker();
            //armatureLinker.LoadLinkFile(PROJECT_PATH + @"\Res\Action\Human-to-Donkey BoneLinker.txt");
            //armatureLinker.LinkRigs(aniRig.Armature, donkeyRig.Armature);
            //_mixamoRotMotionStorage.Transfer(armatureLinker, donkeyRig);

            // -------------------------------------------------
            // 애니메이션 모델에 애니메이션 초기 지정
            foreach (IAnimActor aniActor in _aniActors)
            {
                if (aniActor is Human)
                {
                    (aniActor as Human).SetMotion(HUMAN_ACTION.A_T_POSE);
                }
                else if (aniActor is Donkey)
                {
                    (aniActor as Donkey).SetMotion(DONKEY_ACTION.H_CANTER_RIGHT);
                }
            }


            // 아이템 장착
            //Model3d.TextureStorage.NullTextureFileName = PROJECT_PATH + "\\Res\\debug.jpg";
            //TexturedModel hat = LoadModel(PROJECT_PATH + @"\Res\Items\Merchant_Hat.dae")[0];
            //TexturedModel sword = LoadModel(PROJECT_PATH + @"\Res\Items\cutter_gold.dae")[0];

            //_aniActors[0].EquipItem(ATTACHMENT_SLOT.Head,"hat0", "hat", hat, 200.0f, positionY: -6.0f, pitch:-20);
            //_aniActors[0].EquipItem(ATTACHMENT_SLOT.RightHand, "sword1", "sword", sword, 1.0f, yaw: -90);
            //_aniActors[0].EquipItem(ATTACHMENT_SLOT.LeftHand, "sword0", "sword", sword, 1.0f, yaw: 90);

            //_headLookAt = new SingleBoneLookAt(_aniActors[0].AniRig.Armature["mixamorig_Head"], Vertex3f.UnitZ, Vertex3f.UnitY);
            //_headLookAt = new SingleBoneLookAt(_aniActors[1].AniRig.Armature["Head"], Vertex3f.UnitY, Vertex3f.UnitX);
            //_headLookAt.SetAngleLimits(60, 60);
            //_neckHeadLookAt = ThreeBoneLookAt.CreateNeckHead(_aniActors[0].AniRig.Armature, 
            //localForward: Vertex3f.UnitZ, localUp: Vertex3f.UnitY);

            /*
            _twoLookAt = new TwoBoneLookAt(_aniActors[0].AniRig.Armature["mixamorig_LeftArm"],
                _aniActors[0].AniRig.Armature["mixamorig_LeftForeArm"], 0.99f, Vertex3f.UnitZ, Vertex3f.UnitY);
            //_twoLookAt.SetAngleLimits(20, 20, 90, 90);

            _threeBoneLookAt = new ThreeBoneLookAt(_aniActors[0].AniRig.Armature["mixamorig_LeftArm"],
                _aniActors[0].AniRig.Armature["mixamorig_LeftForeArm"],
                _aniActors[0].AniRig.Armature["mixamorig_LeftHand"],
                0.5f, 0.3f, Vertex3f.UnitZ, Vertex3f.UnitY);


            _twoBoneIK1 = TwoBoneIKFactory.CreateLeftLegIK(_aniActors[0].AniRig.Armature);
            _twoBoneIK2 = TwoBoneIKFactory.CreateLeftLegIK(_aniActors[0].AniRig.Armature);

            _singleBoneRotationIK = SingleBoneRotationIK.Create(_aniActors[0].AniRig.Armature["mixamorig_LeftFoot"],
                Vertex3f.UnitX, Vertex3f.UnitY);

            _twoBoneRotationIK = TwoBoneRotationIK.Create(_aniActors[0].AniRig.Armature["mixamorig_LeftArm"],
                _aniActors[0].AniRig.Armature["mixamorig_LeftForeArm"]);
             */

            _singleLookAt = new SingleBoneLookAt(_aniActors[0].AniRig.Armature[testBone], testForward, testUp);
            _singleLookAt2 = new SingleBoneLookAt(_aniActors[0].AniRig.Armature[testBone2], testForward, testUp);

            _glControl3.CameraStepLength = 0.01f;

            // 셰이더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        Vertex3f[] _vertices;
        Matrix4x4f _temp;

        /// <summary>
        /// 프레임 업데이트를 처리합니다.
        /// </summary>
        /// <param name="deltaTime">이전 프레임과의 시간 간격 (밀리초)</param>
        /// <param name="w">화면 너비</param>
        /// <param name="h">화면 높이</param>
        /// <param name="camera">카메라</param>
        private void UpdateFrame(int deltaTime, int w, int h, Camera camera)
        {
            if (w == 0 || h == 0)
            {
                // 전체 화면 여부 
                _glControl3.SetResolution(this.Width, this.Height);
                _glControl3.FullScreen(false);
            }

            // 시간 간격을 초 단위로 변환
            float duration = deltaTime * 0.001f;

            foreach (IAnimActor aniActor in _aniActors)
            {
                aniActor.Update(deltaTime);
            }


            if (_isLeftTwoBoneIK)
            {
                Vertex3f target = camera.PivotPosition;
                _singleLookAt.Solve(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            }
            else
            {
                Vertex3f target = camera.PivotPosition;
                _singleLookAt2.Solve(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            }

            // 머리가 카메라를 바라보도록 설정
            //_headLookAt.LookAt(new Vertex3f(0,0,1000), _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_headLookAt.SmoothLookAt(camera.PivotPosition, _aniActors[0].ModelMatrix, _aniActors[0].Animator, duration);
            //_twoLookAt.LookAt(camera.PivotPosition, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_twoLookAt.SmoothLookAt(camera.PivotPosition, _aniActors[0].ModelMatrix, _aniActors[0].Animator, duration);
            //_threeLookAt.LookAt(camera.PivotPosition, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_threeLookAt.SmoothLookAt(camera.Position, _aniActors[0].ModelMatrix, _aniActors[0].Animator, duration);

            //_oneLookAt.SolveWorldTarget(camera.PivotPosition, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_oneLookAt.SolveLocalTarget(_aniActors[0].AniRig.Armature.RootBone, camera.PivotPosition, _aniActors[0].ModelMatrix, _aniActors[0].Animator);

            //_twoLookAt.LookAt(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //s_oneLookAt.SolveWorldTarget(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_vertices = _twoBoneIK1.Solve(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_vertices = _singleBoneRotationIK.Solve(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_vertices = _twoBoneRotationIK.Solve(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_singleBoneRotationIK.Solve(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);


            //_oneLookAt.SolveWorldTarget(target, _aniActors[0].ModelMatrix, _aniActors[0].Animator);
            //_temp = _aniActors[0].Animator.RootTransforms[7];


            //_twoBoneIK.SolveRootOnly(camera.PivotPosition, _aniActors[0].ModelMatrix, _aniActors[0].Animator);

            /*
            _glControl3.CLabel("cam").Text = 
                $"CamPos={camera.Position}, " +
                $"CameraPitch={camera.CameraPitch}, " +
                $"CameraYaw={camera.CameraYaw}, " +
                $"Dist={camera.Distance}";
            */

        }

        /// <summary>
        /// 씬을 렌더링합니다.
        /// </summary>
        private void RenderFrame(int deltaTime, float w, float h,  Vertex4f backcolor, Camera camera)
        {
            // 백그라운드 컬러 설정
            //Vertex3f backColor = _glControl3.BackClearColor;

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, (int)w, (int)h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 카메라 중심점 렌더링
            Gl.Enable(EnableCap.DepthTest);

            Matrix4x4f vp = camera.VPMatrix;

            foreach (IAnimActor aniActor in _aniActors)
            {
                aniActor.Render(camera, vp, _animateShader, _staticShader, isBoneVisible: true);
            }

            foreach (IAnimActor aniActor in _aniActors)
            {
                if (aniActor is Human)
                {
                    Renderer3d.RenderBone(_axisShader, _colorShader, camera, aniActor,
                        _aniActors[0].AniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightToeBase], axisLength: 30f);
                    Renderer3d.RenderBone(_axisShader, _colorShader, camera, aniActor,
                        _aniActors[0].AniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftShoulder], axisLength: 30f);
                    Renderer3d.RenderBone(_axisShader, _colorShader, camera, aniActor,
                        _aniActors[0].AniRig.Armature[MIXAMORIG_BONENAME.mixamorig_Spine2], axisLength: 10f);
                    Renderer3d.RenderBone(_axisShader, _colorShader, camera, aniActor,
                        _aniActors[0].AniRig.Armature[testBone], axisLength: 1000f);

                    //Renderer3d.RenderBone(_axisShader, _colorShader, camera, aniActor, _twoBoneIK1.UpperBone, axisLength: 20f);
                    //Renderer3d.RenderBone(_axisShader, _colorShader, camera, aniActor, _twoBoneIK1.LowerBone, axisLength: 15f);
                    //Renderer3d.RenderBone(_axisShader, _colorShader, camera, aniActor, _twoBoneIK1.EndBone, axisLength: 10f);
                }
                break;
            }

            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 0, 0, 1), 0.025f);

            if (_vertices != null)
            {
                if (_vertices.Length > 0)
                    Renderer3d.RenderPoint(_colorShader, _vertices[0], camera, new Vertex4f(0, 1, 0, 1), 0.05f);
                if (_vertices.Length > 1)
                    Renderer3d.RenderPoint(_colorShader, _vertices[1], camera, new Vertex4f(0, 1, 0, 1), 0.05f);
                if (_vertices.Length > 2)
                    Renderer3d.RenderPoint(_colorShader, _vertices[2], camera, new Vertex4f(0, 1, 0, 1), 0.05f);

                // 현재 동작 뼈대
                if (_vertices.Length > 1)
                    Renderer.Renderer3d.RenderLine(_colorShader, camera, _vertices[0], _vertices[1], new Vertex4f(0, 1, 0, 1), 1f);
                if (_vertices.Length > 2)
                    Renderer.Renderer3d.RenderLine(_colorShader, camera, _vertices[1], _vertices[2], new Vertex4f(0, 1, 0, 1), 1f);

                // 폴벡터
                if (_vertices.Length > 3)
                    Renderer.Renderer3d.RenderLine(_colorShader, camera, _vertices[0], _vertices[3], new Vertex4f(0, 1, 1, 1), 1f);

                if (_vertices.Length > 4)
                    Renderer.Renderer3d.RenderLine(_colorShader, camera, _vertices[0], _vertices[4], new Vertex4f(0, 1, 1, 1), 1f);

                if (_vertices.Length > 0)
                    Renderer.Renderer3d.RenderLine(_colorShader, camera, _vertices[0], camera.PivotPosition, new Vertex4f(1, 1, 0, 1), 5f);

            }

            // 폴리곤 모드 설정
            Gl.PolygonMode(MaterialFace.FrontAndBack, _glControl3.PolygonMode);
        }

        public void MouseDnEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
            }
        }

        public void MouseUpEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.NONE;
            }
        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F)
            {
                foreach (IAnimActor aniActor in _aniActors)
                {
                    aniActor.PolygonMode = aniActor.PolygonMode == PolygonMode.Fill ? PolygonMode.Line : PolygonMode.Fill;
                }
            }
            else if (e.KeyCode == Keys.D1)
            {
                for (int i = 0; i < _aniActors.Count; i++)
                {
                    if (_aniActors[i] is Donkey)
                    {
                        (_aniActors[i] as Donkey).SetMotion(DONKEY_ACTION.RANDOM);
                    }
                    if (_aniActors[i] is Human)
                    {
                        (_aniActors[i] as Human).SetMotion(HUMAN_ACTION.RANDOM);
                    }
                }
            }
            else if (e.KeyCode == Keys.D2)
            {
                for (int i = 0; i < _aniActors.Count; i++)
                {
                    if (_aniActors[i] is Donkey)
                    {
                        (_aniActors[i] as Donkey).SetMotion(DONKEY_ACTION.H_NEIGH);
                    }
                    if (_aniActors[i] is Human)
                    {
                        (_aniActors[i] as Human).SetMotion(HUMAN_ACTION.BindPose);
                    }
                }
            }
            else if (e.KeyCode == Keys.D3)
            {
                for(int i = 0; i < _aniActors.Count; i++)
                {
                    if (_aniActors[i] is Donkey)
                    {
                        (_aniActors[i] as Donkey).SetMotion(DONKEY_ACTION.H_CANTER_RIGHT);
                    }
                    if (_aniActors[i] is Human)
                    {
                        (_aniActors[i] as Human).SetMotion(HUMAN_ACTION.BREATHING_IDLE);
                    }
                }
            }
            else if (e.KeyCode == Keys.D4)
            {
                for (int i = 0; i < _aniActors.Count; i++)
                {
                    if (_aniActors[i] is Human)
                    {
                        Human human = _aniActors[i] as Human;
                    }
                }
            }
            else if (e.KeyCode == Keys.D5)
            {
                for (int i = 0; i < _aniActors.Count; i++)
                {
                    if (_aniActors[i] is Human)
                    {
                        Human human = _aniActors[i] as Human;
                        //human.TextNamePlate.CharacterName = "human.CharacterNameplate.CharacterName";
                        //human.TextNamePlate.Refresh();
                    }
                }
            }
            else if (e.KeyCode == Keys.R)
            {
                _isLeftTwoBoneIK = !_isLeftTwoBoneIK;
            }
            else if (e.KeyCode == Keys.D0)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(0, 0, 1.0f);
            }
            else if (e.KeyCode == Keys.M)
            {
                if (_aniActors[0].Animator.IsPlaying)
                {
                    _aniActors[0].Animator.Stop();
                }
                else
                {
                    _aniActors[0].Animator.Play();
                }
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            

        }

        public TexturedModel[] LoadModel(string modelFileName)
        {
            string materialFileName = modelFileName.Replace(".obj", ".mtl");

            // 텍스쳐모델을 읽어온다.
            List<TexturedModel> texturedModels = ObjLoader.LoadObj(modelFileName);

            // 모델에 맞는 원래 모양의 바운딩 박스를 만든다.
            foreach (TexturedModel texturedModel in texturedModels)
            {
                texturedModel.GenerateBoundingBox();
            }

            return texturedModels.ToArray();
        }
    }
}
