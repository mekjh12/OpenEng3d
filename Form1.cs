using Animate;
using Camera3d;
using Fog;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Physics;
using Renderer;
using Shader;
using Sky;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Terrain;
using Ui2d;
using ZetaExt;

namespace OpenEng3d
{
    public partial class Form1 : Form
    {
        enum MOUSE_GAME_MODE { CAMERA_ROUND_ROT, CAMERA_ROUND_ROT2 };

        MOUSE_GAME_MODE _mouseMode = MOUSE_GAME_MODE.CAMERA_ROUND_ROT;

        EngineLoop _gameLoop;

        OcclusionCullingSystem _ocs;
        ShaderGroup _shaders;
        SkyMap _skyBoxMap;
        FogArea _fogArea;
        TerrianPatch _terrain;

        HumanAniModel _humanAniModel;
        AniModel _aniModel;
        AniDae _xmlDae;
        float _duration;
        Texture _billboardTexture;
        Ballistic _ballistic;
        FireworkSet _fireworkSet;
        ParticleForceRegistry _particleForceRegistry;
        bool _isDebugMode = false;


        string _rootPath = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\Res\";
        bool _isEntityRendering = true;

        // 임시
        WorldCoordinate worldCoordinate;

        float _lightTheta = 0.0f;
        Vertex3f _prevPos = Vertex3f.Zero;

        PolygonMode _polygonMode = PolygonMode.Fill;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

            IniFile.SetFileName("setup.ini");

            // ### 초기화 ###
            _gameLoop = new EngineLoop();

            Rand.InitSeed(500);

            _shaders = new ShaderGroup()
            {
                 IsAnimateShader = true,
                 IsBillboardShader = true,
                 IsColorShader = true,
                 IsSkyBoxShader = true,
                 IsStaticShader = true,
            };
            _shaders.Create(EngineLoop.PROJECT_PATH);

            _terrain = new TerrianPatch(EngineLoop.PROJECT_PATH);
            _fogArea = new FogArea();
            _ballistic = new Ballistic();
            _fireworkSet = new FireworkSet();
            _fireworkSet.InitFireworkRules();
            _particleForceRegistry = new ParticleForceRegistry();

            string heightMap = EngineLoop.PROJECT_PATH + @"\Res\209147.png";
            string[] levelTextureMap = new string[5];
            levelTextureMap[0] = EngineLoop.PROJECT_PATH + @"\Res\water1.png";
            levelTextureMap[1] = EngineLoop.PROJECT_PATH + @"\Res\grass_1.png";
            levelTextureMap[2] = EngineLoop.PROJECT_PATH + @"\Res\lowestTile.png";
            levelTextureMap[3] = EngineLoop.PROJECT_PATH + @"\Res\HighTile.png";
            levelTextureMap[4] = EngineLoop.PROJECT_PATH + @"\Res\highestTile.png";
            string detailMap = EngineLoop.PROJECT_PATH + @"\Res\detailMap.png";

            _terrain.Load(heightMap, levelTextureMap, detailMap, n:50, unitSize: 20);
            //_terrain.Load(heightMap, levelTextureMap, detailMap, n: 2, unitSize: 500); // 50*20=1000
            _terrain.ReadOccluder(EngineLoop.PROJECT_PATH + @"\Res\209147.occ3");

            _ocs = new OcclusionCullingSystem(_rootPath);

            _billboardTexture = new Texture(_rootPath + "\\billboard_Tree1Billboard.png", Texture.TextureMapType.Diffuse);

            _ocs.AddRawModel("jochong.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            _ocs.AddRawModel("AxePickaxe.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            _ocs.AddRawModel("lamp.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            _ocs.AddRawModel("pick_stone.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            _ocs.AddRawModel("untitled.obj", OcclusionCullingSystem.BoundingBoxType.AABB);

            _ocs.AddRawModel("brick", Loader3d.LoadCube(2, 2), "bricks.jpg");
            _ocs.AddRawModel("beech_tree_04_sf.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            _ocs.AddRawModel("beech_tree_04_cross_s.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            //_ocs.AddRawModel("grass.obj", aabb: true);
            //_ocs.AddRawModel("detailed_grass_04_3_LOD0.obj", aabb: true);

            //_ocs.AddRawModel("ChungjangGong.obj");
            //_ocs.AddRawModel("Prefab_CB_ThatchedHouseE_HouseB-mesh.obj", aabb: true);
            _ocs.AddRawModel("white_flowers_01_4.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            _ocs.AddRawModel("yellow_flowers_01_4_LOD1.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            _ocs.AddRawModel("grey_willow_04_LOD0.obj", OcclusionCullingSystem.BoundingBoxType.AABB);
            _ocs.AddRawModel("Sunflower_01.obj", OcclusionCullingSystem.BoundingBoxType.AABB);


            float range = 990;

            for (int i = 0; i < 0; i++)
            {
                for (int j = 0; j < 0; j++)
                {
                    _ocs.AddEntity($"brick{i}", "brick", new Vertex3f(-4 * i, -4 * j, 90));
                }
            }

            for (int i = 0; i < 0; i++)
            {
                Vertex2f p = Rand.NextVertex2f(900);
                _ocs.AddEntity($"grass_{i}", "grass", _terrain.GetHeight(p.x, p.y));
            }

            for (int i = 0; i < 0; i++)
            {
                Vertex3f bottomSize2 = _ocs.GetSize("Prefab_CB_ThatchedHouseE_HouseB-mesh");
                float maxSize2 = Math.Max(bottomSize2.x, bottomSize2.y);
                Vertex2f rp = Rand.NextVertex2f(range);
                Vertex3f p = _terrain.GetHeight(rp.x, rp.y);// _terrain.GetPositionRandom(unitSize: maxSize2, size: range);
                _ocs.AddEntity($"Prefab_CB_ThatchedHouseE_HouseB-mesh{i}", "Prefab_CB_ThatchedHouseE_HouseB-mesh", p).IsVisibleOBB = true;
            }

            for (int i = 0; i < 30000; i++) // max=20,000
            {
                Vertex2f rp2 = Rand.NextVertex2f(960);
                Vertex3f p;
                p = _terrain.GetHeight(rp2.x, rp2.y);
                OcclusionEntity occlusionEntity3 = _ocs.AddEntity($"beech_tree_04_sf{i}", "beech_tree_04_sf", p);
                occlusionEntity3.IsLOD = true;
                //occlusionEntity3.IsVisibleRigidBody = true;
                //occlusionEntity3.IsAxisVisible = true;
                occlusionEntity3.SetRigidBody(new Vertex3f(0.07f, 0.07f, 1.0f), new Vertex3f(0.5f, 0.0f, 0.0f));
                float rndTheta = (Rand.NextFloat * 360.0f).ToDegree();
                //occlusionEntity3.Yaw(rndTheta);
                //occlusionEntity3.IsVisibleAABB = true;
                occlusionEntity3.EntityLevel2 = new OcclusionEntity($"beech_tree_04_sf_lod2_{i}", _ocs["beech_tree_04_cross_s"].ToArray())
                {
                    Position = occlusionEntity3.Position,
                    SetPitch = 90,
                };
                (occlusionEntity3.EntityLevel2 as OcclusionEntity).UpdateOBB();
                _ocs.AddTreeBillboard(occlusionEntity3.Position);
            }
            _ocs.UploadTreeBillboardAtGpu();

            for (int i = 0; i < 2000; i++)
            {
                Vertex2f rp = Rand.NextVertex2f(range);
                _ocs.AddEntity($"Sunflower_01{i}", "Sunflower_01",
                    _terrain.GetHeight(rp.x, rp.y));

                Vertex2f rp1 = Rand.NextVertex2f(range);
                _ocs.AddEntity($"white_flowers_01_4{i}", "white_flowers_01_4",
                    _terrain.GetHeight(rp1.x, rp1.y));

                Vertex2f rp2 = Rand.NextVertex2f(range);
                _ocs.AddEntity($"grey_willow_04_LOD0{i}", "grey_willow_04_LOD0",
                    _terrain.GetHeight(rp2.x, rp2.y));

                Vertex2f rp3 = Rand.NextVertex2f(range);
                _ocs.AddEntity($"Sunflower_01{i}", "Sunflower_01",
                    _terrain.GetHeight(rp3.x, rp3.y));
            }

            // 애니메이션
            EngineLoop.cameraSpeed = 3.0f;

            _xmlDae = new AniDae(EngineLoop.PROJECT_PATH + $"\\Res\\abe.dae", isLoadAnimation: false);

            // *** Action ***
            foreach (string fn in Directory.GetFiles(EngineLoop.PROJECT_PATH + "\\Res\\Action\\default\\"))
            {
                if (Path.GetExtension(fn) == ".dae")
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(fn);
                    string motionName = Path.GetFileNameWithoutExtension(fn);
                    AniXmlLoader.LoadMixamoMotion(_xmlDae, xml, motionName);
                    Console.WriteLine($"LoadMixamoMotion = {motionName}");
                }
            }

            Entity daeEntity = new Entity("aniModel", _xmlDae.Models.ToArray());
            daeEntity.Material = Material.White;
            daeEntity.IsAxisVisible = true;

            _humanAniModel = new HumanAniModel("main", daeEntity, _xmlDae);
            _humanAniModel.Transform.SetPosition(new Vertex3f(0, 1, 0));
            _humanAniModel.SetMotion(HumanAniModel.ACTION.IDLE);

            //_ocs.BakeOptimizationByAABBSize();

            /*
            Vertex3f bottomSize = _ocs.GetSize("detailed_grass_04_3_LOD0");
            float maxSize = Math.Max(bottomSize.x, bottomSize.y);
            for (int i = 0; i < 1; i++)
            {
                while (true)
                {
                    float rx = Rand.Next(-range, range);
                    float ry = Rand.Next(-range, range);
                    if (_terrain.DegreeOfSlope(rx, ry, maxSize) > 40)
                    {
                        _ocs.AddEntity($"detailed_grass_04_3_LOD0{i}", "detailed_grass_04_3_LOD0", _terrain.GetHeight(rx,ry));
                        break;
                    }
                }

                while (true)
                {
                    float rx = Rand.Next(-range, range);
                    float ry = Rand.Next(-range, range);
                    if (_terrain.DegreeOfSlope(rx, ry, maxSize) > 50)
                    {
                        _ocs.AddEntity($"grass_{i}", "grass", _terrain.GetHeight(rx, ry));
                        break;
                    }
                }
            }
            */

            //_ocs.Print();

            FontFamilySet.AddFonts(EngineLoop.EXECUTE_PATH + "\\fonts\\fontList.txt");
            UIEngine.REOURCES_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\UIDesign2d\Res\";
            UITextureLoader.LoadTexture2d(UIEngine.REOURCES_PATH);

            _gameLoop.InitFrame += (w, h) =>
            {
                Console.WriteLine("game loop init!");
                UIEngine.Add(new UIEngine("mainUI", w, h) { AlwaysRender = true }, w, h);
                UIEngine.DesignInit += (w1, h1) =>
                {
                    UIEngine.AddControl("mainUI", new Ui2d.Label("help", FontFamilySet.연성체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.ROOT_BR,
                        IsCenter = true,
                        Margin = 0.010f,
                        FontSize = 1.0f,
                        Alpha = 0.1f,
                        ForeColor = new Vertex3f(1, 0, 0),
                        BackColor = new Vertex3f(1, 1, 1),
                        BorderColor = new Vertex3f(1, 0, 0),
                        BorderWidth = 1.0f,
                        IsBorder = true,
                        Text = "occ=1,2 cam speed=4,5 fog density=6,7 height=8,9",
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("msg", FontFamilySet.연성체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.ROOT_BL,
                        IsCenter = true,
                        Margin = 0.010f,
                        FontSize = 1.1f,
                        Alpha = 0.1f,
                        ForeColor = new Vertex3f(1, 0, 0),
                        BackColor = new Vertex3f(1, 1, 1),
                        BorderColor = new Vertex3f(1, 0, 0),
                        BorderWidth = 1.0f,
                        IsBorder = true,
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("fps", FontFamilySet.조선100년체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.ROOT_TL,
                        IsCenter = true,
                        Margin = 0.010f,
                        FontSize = 1.3f,
                        Alpha = 0.1f,
                        ForeColor = new Vertex3f(1, 0, 0),
                        BackColor = new Vertex3f(1, 1, 1),
                        BorderColor = new Vertex3f(1, 0, 0),
                        BorderWidth = 1.0f,
                        IsBorder = true,
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("hasCount", FontFamilySet.조선100년체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                        AdjontControl = UIEngine.UI2d<Ui2d.Label>("fps"),
                        ForeColor = new Vertex3f(1, 0, 0),
                        FontSize = 1.1f,
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("renderCount", FontFamilySet.조선100년체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                        AdjontControl = UIEngine.UI2d<Ui2d.Label>("hasCount"),
                        ForeColor = new Vertex3f(1, 0, 0),
                        FontSize = 1.1f,
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("occlusionCount", FontFamilySet.조선100년체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                        AdjontControl = UIEngine.UI2d<Ui2d.Label>("renderCount"),
                        ForeColor = new Vertex3f(1, 0, 0),
                        FontSize = 1.1f,
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("occulusionTerrainCount", FontFamilySet.연성체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                        AdjontControl = UIEngine.UI2d<Ui2d.Label>("occlusionCount"),
                        ForeColor = new Vertex3f(1, 0, 0),
                        FontSize = 1.1f,
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("cameraPosition", FontFamilySet.연성체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                        AdjontControl = UIEngine.UI2d<Ui2d.Label>("occulusionTerrainCount"),
                        ForeColor = new Vertex3f(1, 0, 0),
                        FontSize = 1.1f,
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("cameraOrbitPosition", FontFamilySet.연성체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                        AdjontControl = UIEngine.UI2d<Ui2d.Label>("cameraPosition"),
                        ForeColor = new Vertex3f(1, 0, 0),
                        FontSize = 1.1f,
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("console", FontFamilySet.연성체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                        AdjontControl = UIEngine.UI2d<Ui2d.Label>("cameraOrbitPosition"),
                        ForeColor = new Vertex3f(1, 0, 0),
                        FontSize = 1.1f,
                        Text = "debug"
                    });

                    UIEngine.AddControl("mainUI", new Ui2d.Label("consoleFrame", FontFamilySet.연성체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                        AdjontControl = UIEngine.UI2d<Ui2d.Label>("console"),
                        ForeColor = new Vertex3f(1, 1, 0),
                        FontSize = 1.1f,
                        Text = "debug"
                    });

                    UIEngine.AddControl("mainUI", new ImageButton("btn", FontFamilySet.연성체)
                    {
                        Align = Ui2d.Control.CONTROL_ALIGN.ROOT_TR,
                        Margin = 0.010f,
                        Size = new Vertex2f(0.12f, 0.12f) * 0.5f,
                        ForeColor = new Vertex3f(1, 0, 0),
                        BackColor = new Vertex3f(1, 1, 1),
                        BorderColor = new Vertex3f(1, 0, 0),
                        BackgroundImage = UITextureLoader.Transparent,
                        ItemImageID = UITextureLoader.Texture("Button13281"),
                        BorderWidth = 1.0f,
                        IsBorder = true,
                        FontSize = 1.0f,
                        Text = "버튼샘플",
                    });
                };

                UIEngine.InitFrame(w, h);
                UIEngine.StartFrame();
                UIEngine.EnableMouse = true;
            };

            // 카메라 설정
            float cx = float.Parse(IniFile.GetPrivateProfileString("camera", "x", "0.0"));
            float cy = float.Parse(IniFile.GetPrivateProfileString("camera", "y", "0.0"));
            float cz = float.Parse(IniFile.GetPrivateProfileString("camera", "z", "0.0"));
            float yaw = float.Parse(IniFile.GetPrivateProfileString("camera", "yaw", "0.0"));
            float pitch = float.Parse(IniFile.GetPrivateProfileString("camera", "pitch", "0.0"));
            float dist = float.Parse(IniFile.GetPrivateProfileString("camera", "dist", "1.0"));
            _gameLoop.Camera = new OrbitCamera("", cx, cy, cz, dist);
            _gameLoop.Camera.CameraPitch = pitch;
            _gameLoop.Camera.CameraYaw = yaw;

            // ### 주요로직 ###
            _gameLoop.UpdateFrame = (deltaTime) =>
            {
                int w = this.glControl1.Width;
                int h = this.glControl1.Height;
                if (_gameLoop.Width * _gameLoop.Height == 0)
                {
                    // 초기화 부분이다.
                    _gameLoop.Init(w, h);
                    _gameLoop.Camera.Init(w, h);
                    UIEngine.EnableMouse = false;
                    worldCoordinate = new WorldCoordinate(800);

                    _skyBoxMap = new SkyMap(new string[]
                    {
                        _rootPath + "skybox\\right.jpg",
                        _rootPath + "skybox\\left.jpg",
                        _rootPath + "skybox\\front.jpg",
                        _rootPath + "skybox\\back.jpg",
                        _rootPath + "skybox\\top.jpg",
                        _rootPath + "skybox\\bottom.jpg",
                    });
                    _skyBoxMap.Init();
                }

                float duration = deltaTime * 0.001f;
                _duration = duration;

                Debug.ClearFrameText();
                Debug.Write(_humanAniModel.CurrentHandItem.ToString());

                // 물리부분 (카메라 우선 지정--> 캐릭터위치)
                OrbitCamera camera = (OrbitCamera)_gameLoop.Camera;
                float height = _terrain.GetHeight(camera.Position);
                _humanAniModel.Transform.SetPosition(new Vertex3f(camera.Position.x, camera.Position.y, height));
                camera.Position = _humanAniModel.Transform.Position + new Vertex3f(0, 0, 1.6f);
                OrbitCamera orbitCamera = (camera as OrbitCamera);
                float orbitCameraHeight = _terrain.GetHeight(orbitCamera.OrbitPosition) + OrbitCamera.Epsilon;
                if (orbitCamera.OrbitPosition.z < orbitCameraHeight)
                {
                    orbitCamera.OrbitPosition = orbitCamera.ModifyOrbitPostionByTerrain(orbitCameraHeight);
                }
                float velocity = ((camera.Position - _prevPos).Distance() * 0.001f) / (3600.0f * deltaTime);
                this.Text = $"{FramePerSecond.FPS}fps, h={height}, t={FramePerSecond.GlobalTick} p={camera.Position} v={velocity} d={_prevPos} detailmap:R, light:d1d2";
                _prevPos = camera.Position;


                float g = 1.0f / (float)Math.Tan((camera.FOV * 0.5f).ToRadian());
                Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera.OrbitPosition,
                    camera.Forward, camera.Up, camera.Right, g, camera.AspectRatio, camera.NEAR, camera.FAR);

                Polyhedron viewFrustumTerrain = ViewFrustum.BuildFrustumPolyhedron(camera.OrbitPosition,
                    camera.Forward, camera.Up, camera.Right, g, camera.AspectRatio, camera.NEAR, camera.FAR);

                _terrain.Update(camera, viewFrustumTerrain, _ocs);

                _ocs.UpdateRigid(camera, _humanAniModel.Collider, out Node contactNode1);

                OcclusionEntity axe = (OcclusionEntity)_humanAniModel.RightHandEntity;
                if (axe != null)
                {
                    //axe.UpdateOBB();
                    //axe.UpdateOBB();
                    //Debug.Write(" 도끼행렬=" + axe.RigidBody.ToString());
                    //Debug.Write(" ocs test==>");

                    axe.SetRigidBody(Vertex3f.One, Vertex3f.Zero);

                    if (axe.RigidBody != null)
                    {
                        Matrix4x4f mat = _humanAniModel.Transform.Matrix4x4f * _humanAniModel.BoneAnimationTransforms[53] * axe.LocalBindMatrix;
                        Vertex3f p = new Vertex3f(0, 0, -0.5f);
                        Vertex3f f = (mat * p).Vertex3f();
                        Vertex3f l = new Vertex3f(0.05f, 0.05f, 0.05f);
                        AABB aabb = new AABB(f - l, f + l);
                        if (_ocs.UpdateCollisionRigid(camera, aabb, out Node contactNode))
                        {
                            Debug.Write(" 도끼맞음");
                            if (contactNode != null)
                            {
                                if (_humanAniModel.CurrentMotion.Name == "Axe Attack Downward")
                                {
                                    //contactNode.AABB.OcclusionEntity.Pitch(0.2f);
                                    (contactNode.AABB.PrimitiveEntity as OcclusionEntity).ScaleDelta(0.9f, 0.9f, 0.9f);
                                }
                            }
                        }
                    }

                    

                }
                _ocs.Update(camera, viewFrustum, _fogArea.FogPlane, _fogArea.FogDensity);

                _ballistic.Update(duration);
                _fireworkSet.Update(duration);

                _particleForceRegistry.UpdateForces(duration);
                _particleForceRegistry.Update(duration);

                Debug.Write($"입자수={_particleForceRegistry.ParticleCount}, 힘개수={_particleForceRegistry.ParticleForceCount}");

                _skyBoxMap.Update(camera, deltaTime);

                // 애니메이션
                _humanAniModel.Update(deltaTime);

                //Polyhedron poly = Polyhedron.BiPlane(new Vertex3f[] { new Vertex3f(0, 0, 0), new Vertex3f(0, 1, 0), new Vertex3f(1, 1, 0), new Vertex3f(1, 0, 0) }, new Polygon(new uint[] { 0, 1, 2, 3 }));
                //poly.Transform(x:-24, y:-7, z:83, rotdegX: 10);
                //_viewFrustum = BakeModel3d.MakeEntity("viewFrustum_Polyhedron", poly, new Vertex4f(1, 1, 0, 0.25f));
                //_viewFrustum.Position = camera.Position;

                int glLeft = this.Width - this.glControl1.Width;
                int glTop = this.Height - this.glControl1.Height;
                int glWidth = this.glControl1.Width;
                int glHeight = this.glControl1.Height;

                UIEngine.UI2d<Ui2d.Label>("fps").Text = "프레임율(" + FramePerSecond.FPS + "fps)";
                UIEngine.UI2d<Ui2d.Label>("hasCount").Text = "보유수(" + _ocs.EntityCount + "개)";
                UIEngine.UI2d<Ui2d.Label>("renderCount").Text = "렌더링수(" + _ocs.FrustumPassEntity?.Count + "개)";
                UIEngine.UI2d<Ui2d.Label>("occlusionCount").Text = "오클루전통과수(" + _ocs.OccludedEntity?.Count + "개)";
                UIEngine.UI2d<Ui2d.Label>("cameraPosition").Text = $"cam pos={camera.Position.x}/{camera.Position.y}/{camera.Position.z}";
                UIEngine.UI2d<Ui2d.Label>("cameraOrbitPosition").Text = $"OrbitPosition={camera.OrbitPosition.x}/{camera.OrbitPosition.y}/{camera.OrbitPosition.z}";
                UIEngine.UI2d<Ui2d.Label>("console").Text = $"경사도={_terrain.DegreeOfSlope(camera.Position.x, camera.Position.y, 20)}";
                UIEngine.UI2d<Ui2d.Label>("msg").Text = "디버깅=" + Debug.Text;
                UIEngine.UI2d<Ui2d.Label>("consoleFrame").Text = Debug.TextFrame;
                UIEngine.MouseUpdateFrame(this.Left + glLeft, this.Top + glTop, glWidth, glHeight, 0);
                UIEngine.UpdateFrame(deltaTime);

            };

            _gameLoop.RenderFrame = (deltaTime) =>
            {
                Camera camera = _gameLoop.Camera;

                Gl.Enable(EnableCap.CullFace);
                Gl.CullFace(CullFaceMode.Back);

                Vertex3f fogColor = _fogArea.Color;
                Gl.ClearColor(fogColor.x, fogColor.y, fogColor.z, 1.0f);
                Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                Gl.Enable(EnableCap.DepthTest);

                Gl.PolygonMode(MaterialFace.Front, _polygonMode);

                Vertex3f lightDirection = new Vertex3f((float)Math.Cos(_lightTheta.ToRadian()),
                    (float)Math.Sin(_lightTheta.ToRadian()), (float)Math.Cos(_lightTheta.ToRadian())).Normalized;

                _skyBoxMap.Render(_shaders.SkyBoxShader, camera, _fogArea.Color, _fogArea.FogPlane, _fogArea.FogDensity);

                Renderer3d.Render(_shaders.ColorShader, camera, worldCoordinate);

                _terrain.Render(camera, lightDirection, _shaders.StaticShader, _shaders.ColorShader, _fogArea.Color, _fogArea.FogPlane, _fogArea.FogDensity);
                foreach (OcclusionEntity entity in _terrain.TerrainOccluder)
                {
                    if (entity.IsVisibleOBB)
                    {
                        Renderer3d.RenderOBB(_shaders.ColorShader, entity.OBB, new Vertex4f(1, 1, 0, 0.3f), camera);
                    }
                }

                Renderer3d.Render(_shaders.BillboardShader, _ocs.TreeBillboardVAO, _ocs.TreeBillboardCount, _billboardTexture.TextureID, camera,
                    _fogArea.Color, _fogArea.FogPlane, _fogArea.FogDensity);

                // 애니메이션
                _humanAniModel.PolygonMode = _polygonMode;
                _humanAniModel.Render(camera, _shaders.StaticShader, _shaders.AnimateShader, isSkinVisible: true, isBoneVisible: false,
                     isBoneParentCurrentVisible: false);

                // 입자 물리
                _particleForceRegistry.Render(Renderer3d.Cube.VAO, Renderer3d.Cube.VertexCount, _shaders.ColorShader, camera.ProjectiveMatrix, camera.ViewMatrix);
                _ballistic.Render(Renderer3d.Cube.VAO, Renderer3d.Cube.VertexCount, _shaders.ColorShader, camera.ProjectiveMatrix, camera.ViewMatrix);
                _fireworkSet.Render(Renderer3d.Cube.VAO, Renderer3d.Cube.VertexCount, _shaders.ColorShader, camera.ProjectiveMatrix, camera.ViewMatrix);

                // 투명한 것은 나중에 렌더링
                Gl.Enable(EnableCap.Blend);
                Gl.BlendEquation(BlendEquationMode.FuncAdd);
                Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                Gl.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);
                Gl.LineWidth(3.0f);
                Gl.Disable(EnableCap.CullFace);

                foreach (TerrainOccluder3 occluder in _terrain.TerrainOccluders)
                {
                    Renderer3d.RenderTerrainOcc(_shaders.ColorShader, occluder, new Vertex4f(1, 0, 0, 0.3f), camera);
                }
                Gl.LineWidth(1.0f);

                Gl.Enable(EnableCap.CullFace);
                Gl.PolygonMode(MaterialFace.Front, _polygonMode);
                if (_isEntityRendering)
                {
                    foreach (OcclusionEntity entity in _ocs.OccludedEntity)
                    {
                        Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, entity, camera, _fogArea.Color, _fogArea.FogPlane, _fogArea.FogDensity);
                        if (entity.IsVisibleAABB)
                        {
                            Renderer3d.RenderAABB(_shaders.ColorShader, entity.AABB, entity.AABB.Color.xyzw(0.3f), camera);
                        }
                        if (entity.IsVisibleRigidBody)
                        {
                            Renderer3d.RenderAABB(_shaders.ColorShader, entity.RigidBody, entity.AABB.Color.xyzw(0.3f), camera);
                        } 
                    }
                }

                OcclusionEntity axe = _humanAniModel.RightHandEntity as OcclusionEntity;
                if (axe != null)
                {
                    if (axe.IsVisibleRigidBody)
                    {
                        Matrix4x4f mat = _humanAniModel.Transform.Matrix4x4f * _humanAniModel.BoneAnimationTransforms[53] * axe.LocalBindMatrix;
                        Vertex3f p = new Vertex3f(0, 0, -0.5f);
                        Vertex3f f = (mat * p).Vertex3f();
                        Renderer3d.RenderPoint(_shaders.ColorShader, f, camera, axe.AABB.Color.xyzw(0.8f));
                        Renderer3d.RenderAABB(_shaders.ColorShader, _humanAniModel.Transform.Matrix4x4f, _humanAniModel.BoneAnimationTransforms[53], 
                            axe.LocalBindMatrix, axe.LocalOBB.ModelMatrix, axe.AABB.Color.xyzw(0.8f), camera);
                    }
                }

                //foreach (OcclusionEntity entity in _ocs.OccludedEntity)
                {
                    //if (entity.IsVisibleOBB)
                    {
                        //Renderer3d.RenderOBB(_cShader, entity.OBB, new Vertex4f(1, 1, 0, 0.3f), camera);
                    }

                    //if (entity.IsVisibleAABB)
                    {
                        //Renderer3d.RenderAABB(_cShader, entity.AABB, entity.AABB.Color.xyzw(0.3f), camera);
                    }
                }

                //foreach (AABB aabb in _ocs.AABBNodes)
                {
                    //Renderer3d.RenderAABB(_cShader, aabb, aabb.Color.xyzw(0.1f), camera);
                }

                Gl.Disable(EnableCap.Blend);

                Gl.PolygonMode(MaterialFace.Front, PolygonMode.Fill);

                // 중심점
                Gl.Disable(EnableCap.DepthTest);
                Renderer3d.RenderPoint(_shaders.ColorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
                Gl.Enable(EnableCap.DepthTest);

                UIEngine.RenderFrame(deltaTime);
            };
        }

        private void glControl1_KeyDown(object sender, KeyEventArgs e)
        {
            OrbitCamera camera = (OrbitCamera)_gameLoop.Camera;

            if (e.KeyCode == Keys.ShiftKey)
            {
                Debug.Write("shiftkey press!");
            }

            if (e.KeyCode == Keys.Escape)
            {
                if (MessageBox.Show("정말로 끝내시겠습니까?", "종료", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // 종료 설정 저장
                    IniFile.WritePrivateProfileString("camera", "x", _gameLoop.Camera.Position.x);
                    IniFile.WritePrivateProfileString("camera", "y", _gameLoop.Camera.Position.y);
                    IniFile.WritePrivateProfileString("camera", "z", _gameLoop.Camera.Position.z);
                    IniFile.WritePrivateProfileString("camera", "yaw", _gameLoop.Camera.CameraYaw);
                    IniFile.WritePrivateProfileString("camera", "pitch", _gameLoop.Camera.CameraPitch);
                    IniFile.WritePrivateProfileString("camera", "dist", (_gameLoop.Camera as OrbitCamera).Distance);
                    Application.Exit();
                }
            }
            else if (e.KeyCode == Keys.F)
            {
                _polygonMode = (_polygonMode == PolygonMode.Fill) ?
                    PolygonMode.Line : PolygonMode.Fill;
            }
            else if (e.KeyCode == Keys.D1)
            {
                _humanAniModel.FoldHand(Mammal.BODY_PART.RightHand);
            }
            else if (e.KeyCode == Keys.D2)
            {
                _humanAniModel.UnfoldHand(Mammal.BODY_PART.RightHand);
            }
            else if (e.KeyCode == Keys.D3)
            {
                
            }
            else if (e.KeyCode == Keys.D4)
            {
                for (int i = 0; i < 250; i++)
                {
                    Particle particle = new Particle()
                    {
                        Position = camera.Position,
                        Velocity = (Rand.NextColor3f * 2.0f - new Vertex3f(-1, -1, -1)) * Rand.Next(2.0f, 10.0f),
                        Life = Rand.Next(1.0f, 6.0f),
                    };
                    ParticleGravity gravity = new ParticleGravity();
                    _particleForceRegistry.AddParticle(particle);
                    _particleForceRegistry.AddForce(particle, gravity);
                }

                if (e.KeyCode == Keys.LShiftKey)
                {
                    
                }
                else
                {
                    //_fireworkSet.Create(1, 10, camera.Position, null);
                }
            }
            else if (e.KeyCode == Keys.D5)
            {
                _humanAniModel.RemoveItem(Mammal.BODY_PART.RightHand);
                List<TexturedModel> texturedModels = _ocs.GetRawModels("jochong");
                OcclusionEntity jochongEntity = new OcclusionEntity("jochong_entity", texturedModels);
                _humanAniModel.Attach(Mammal.BODY_PART.RightHand, jochongEntity);
                _humanAniModel.SetMotion(HumanAniModel.ACTION.IDLE);
                _humanAniModel.CurrentHandItem = HumanAniModel.HAND_ITEM.GUN;
            }
            else if (e.KeyCode == Keys.D6)
            { 
                _humanAniModel.RemoveItem(Mammal.BODY_PART.RightHand);
                List<TexturedModel> texturedModels = _ocs.GetRawModels("pick_stone");
                OcclusionEntity AxePickaxe = new OcclusionEntity("pick_stone_entity", texturedModels);
                AxePickaxe.GenBoxOccluder();
                AxePickaxe.IsVisibleRigidBody = true;
                _humanAniModel.Attach(Mammal.BODY_PART.RightHand, AxePickaxe);
                _humanAniModel.CurrentHandItem = HumanAniModel.HAND_ITEM.AXE;
                _humanAniModel.SetMotion(HumanAniModel.ACTION.IDLE);
            }
            else if (e.KeyCode == Keys.D7)
            {
               
            }
            else if (e.KeyCode == Keys.D8)
            {
            }
            else if (e.KeyCode == Keys.D9)
            {
                if (e.KeyCode == Keys.LShiftKey)
                {
                    _fogArea.FogDensity *= 1.1f;
                }
                else
                {
                    _fogArea.FogPlane = new Vertex4f(_fogArea.FogPlane.x, _fogArea.FogPlane.y, _fogArea.FogPlane.z, _fogArea.FogPlane.w + 0.5f);
                }
            }
            else if (e.KeyCode == Keys.D0)
            {
                if (e.KeyCode == Keys.LShiftKey)
                {
                    _fogArea.FogDensity /= 1.1f;
                }
                else
                {
                    _fogArea.FogPlane = new Vertex4f(_fogArea.FogPlane.x, _fogArea.FogPlane.y, _fogArea.FogPlane.z, _fogArea.FogPlane.w - 0.5f);
                }
            }
            else if (e.KeyCode == Keys.R)
            {
                _isEntityRendering = !_isEntityRendering;
            }
        }

        private void glControl1_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {

        }

        private void glControl1_Render(object sender, GlControlEventArgs e)
        {
            int glLeft = this.Width - this.glControl1.Width;
            int glTop = this.Height - this.glControl1.Height;
            int glWidth = this.glControl1.Width;
            int glHeight = this.glControl1.Height;
            _gameLoop.DetectInput(this.Left + glLeft, this.Top + glTop, glWidth, glHeight);

            // 엔진 루프, 처음 로딩시 deltaTime이 커지는 것을 방지
            if (FramePerSecond.DeltaTime < 1000)
            {
                _gameLoop.Update(deltaTime: FramePerSecond.DeltaTime, _humanAniModel);
                _gameLoop.Render(deltaTime: FramePerSecond.DeltaTime);
            }
            FramePerSecond.Update();
        }

        private void glControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            Camera camera = _gameLoop.Camera;
            if (camera is FPSCamera) camera?.GoForward(0.02f * e.Delta);
            if (camera is OrbitCamera) (camera as OrbitCamera)?.FarAway(-0.005f * e.Delta);
        }

        private void glControl1_MouseMove(object sender, MouseEventArgs e)
        {
            Mouse.CurrentPosition = new Vertex2i(e.X, e.Y);
            Vertex2i delta = Mouse.DeltaPosition;

            Camera camera = _gameLoop.Camera;

            if (_mouseMode == MOUSE_GAME_MODE.CAMERA_ROUND_ROT)
            {
                // 카메라를 회전
                camera?.Yaw(-delta.x);
                camera?.Pitch(delta.y);

                // 캐릭터를 회전
                _humanAniModel.Transform.SetForward(camera.Forward);
            }
            else if (_mouseMode == MOUSE_GAME_MODE.CAMERA_ROUND_ROT2)
            {
                // 카메라를 회전
                camera?.Yaw(-delta.x);
                camera?.Pitch(delta.y);
            }

            Mouse.PrevPosition = new Vertex2i(e.X, e.Y);
        }

        float yaw = 0.0f;
        float pitch = 0.0f;

        private void glControl1_MouseDown(object sender, MouseEventArgs e)
        {
            OrbitCamera camera = (OrbitCamera)_gameLoop.Camera;

            if (e.Button == MouseButtons.Left)
            {
                Vertex3f goForward = _humanAniModel.Transform.ForwardAlignFloor;
                _humanAniModel.HandAction();
                _humanAniModel.SetMotionImmediately(HumanAniModel.ACTION.STOP);

                if (_humanAniModel.CurrentHandItem == HumanAniModel.HAND_ITEM.GUN)
                {
                    int shotType = Rand.Next(1, 3);
                    Particle particle = _ballistic.CreateParticle((Ballistic.ShotType)shotType, _humanAniModel.RightHandPosition, goForward);
                    _particleForceRegistry.AddParticle(particle);
                    _particleForceRegistry.AddForce(particle, new ParticleDrag(0.5f, 0.1f));
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                yaw = camera.CameraYaw;
                pitch = camera.CameraPitch;
                _mouseMode = MOUSE_GAME_MODE.CAMERA_ROUND_ROT2;
            }
        }

        private void glControl1_MouseUp(object sender, MouseEventArgs e)
        {
            OrbitCamera camera = (OrbitCamera)_gameLoop.Camera;

            if (e.Button == MouseButtons.Left)
            {

            }
            else if (e.Button == MouseButtons.Right)
            {
                camera.CameraYaw = yaw;
                camera.CameraPitch = pitch;
                _mouseMode = MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
            }
        }
    }
}
