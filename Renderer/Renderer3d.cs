using Camera3d;
using Common;
using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using ZetaExt;
using static OpenGL.Gl;

namespace Renderer
{
    /// <summary>
    /// 3D 렌더링을 위한 정적 유틸리티 클래스입니다.
    /// 다양한 3D 모델과 렌더링 메서드를 제공합니다.
    /// </summary>
    public static class Renderer3d
    {
        #region 기본 3D 모델
        public static BaseModel3d Point = Loader3d.LoadPoint(0, 0, 0);
        /// <summary>기본 라인 모델</summary>
        public static BaseModel3d Line = Loader3d.LoadLine(0, 0, 0, 1, 0, 0);
        /// <summary>Z축 방향 라인 모델</summary>
        public static BaseModel3d LineZ = Loader3d.LoadLine(0, 0, 0, 0, 0, 1);
        /// <summary>기본 큐브 모델</summary>
        public static BaseModel3d Cube = Loader3d.LoadCube();
        /// <summary>기본 원뿔 모델 (4면체, 높이 3.0)</summary>
        public static BaseModel3d Cone = Loader3d.LoadCone(4, 1.0f, 3.0f, false);
        /// <summary>기본 구체 모델 (반지름 1, 6면체)</summary>
        public static BaseModel3d Sphere = Loader3d.LoadSphere(r: 1, piece: 6);
        /// <summary>지구본 모델 (반지름 1, 20면체)</summary>
        public static BaseModel3d Earth = Loader3d.LoadSphere(r: 1,  piece: 30, outer: false);
        //public static BaseModel3d Earth = Loader3d.LoadHalfUppperSphere(r: 1, horzPicesCount:36, piece: 200, startpicesIndex: 170, endpicesIndex:200, outer: false);
        /// <summary>기본 사각형 모델</summary>
        public static BaseModel3d Rect = Loader3d.LoadPlane();
        /// <summary>기본 쿼드 모델</summary>
        public static BaseModel3d Quad = Loader3d.LoadQuad();
        /// <summary>기본 좌표축 모델</summary>
        public static BaseModel3d Axis = Loader3d.LoadAxis(1);
        /// <summary>기본 실린더 모델 (12면체)</summary>
        public static BaseModel3d Cylinder = Loader3d.LoadPrism(12, 1, 1, 1, Matrix4x4f.Identity);
        /// <summary>기본 실린더 모델 (12면체)</summary>
        public static RawModel3d QaudPatch = Loader3d.LoadQuadPatch();

        #endregion

        public static void Render(UnlitShader shader, List<Entity> entities, Camera camera, bool isCullface = true)
        {
            OrbitCamera orbitCamera = camera as OrbitCamera;
            shader.Bind();

            Matrix4x4f vp = orbitCamera.VPMatrix;

            foreach (Entity entity in entities)
            {
                if (entity.IsDrawOneSide == false)
                {
                    Gl.Disable(EnableCap.CullFace);
                }
                else
                {
                    Gl.Enable(EnableCap.CullFace);
                    Gl.CullFace(CullFaceMode.Back);
                }

                shader.LoadUniform(UnlitShader.UNIFORM_NAME.mvp, vp * entity.ModelMatrix);

                BaseModel3d[] models = (entity is LodEntity) ? (entity as LodEntity).Models.ToArray() : entity.Models.ToArray();

                // 모델을 그린다.
                foreach (BaseModel3d rawModel in models)
                {
                    if (entity.IsTextured)
                    {
                        TexturedModel modelTextured = rawModel as TexturedModel;
                        if (modelTextured.Texture != null)
                        {
                            if (modelTextured.Texture.TextureType.HasFlag(Texture.TextureMapType.Diffuse))
                            {
                                shader.LoadTexture( UnlitShader.UNIFORM_NAME.modelTexture, TextureUnit.Texture0, modelTextured.Texture.DiffuseMapID);
                            }
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                        }
                    }

                    Gl.BindVertexArray(rawModel.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.EnableVertexAttribArray(1);
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);
                    Gl.DisableVertexAttribArray(1);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }
            }

            shader.Unbind();
        }

        public static void Render(BillboardShader shader, uint vao, int count, uint textureId, Camera camera,
            Vertex3f fogColor, Vertex4f fogPlane, float fogDensity)
        {
            shader.Bind();

            Matrix4x4f VP = camera.ProjectiveMatrix * camera.ViewMatrix; //순서는 오른쪽부터 왼쪽으로
            //shader.LoadViewProjMatrix(VP);
            shader.LoadFogColor(fogColor);
            shader.LoadFogDensity(fogDensity);
            shader.LoadFogPlane(fogPlane);
            shader.LoadViewMatrix(camera.ViewMatrix);
            shader.LoadProjMatrix(camera.ProjectiveMatrix);
            shader.LoadCameraPosition(camera.Position);
            shader.LoadTexture(textureId);

            //gColorMap

            Gl.BindVertexArray(vao);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Points, 0, count);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            shader.Unbind();
        }


        public static void Render(ImpostorLODSystem impostor, ImpostorShader shader, List<Entity> impostorEntity, Camera camera)
        {
            Gl.Disable(EnableCap.Blend);

            OrbitCamera orbitCamera = camera as OrbitCamera;
            shader.Bind();

            shader.LoadUniform(ImpostorShader.UNIFORM_NAME.enableEdgeLine, true);
            shader.LoadUniform(ImpostorShader.UNIFORM_NAME.vp, orbitCamera.VPMatrix);
            shader.LoadUniform(ImpostorShader.UNIFORM_NAME.cameraPosition, orbitCamera.Position);

            foreach (Entity entity in impostorEntity)
            {
                ImpostorSettings settings = impostor.GetImpostorSettings(entity);
                Vertex2f atlasOffset = impostor.GetAtlasOffset(settings, orbitCamera.Position, entity);
                uint textureId = impostor.AtlasTexture(entity);

                shader.LoadTexture(ImpostorShader.UNIFORM_NAME.impostorAtlas, TextureUnit.Texture0, textureId);
                shader.LoadUniform(ImpostorShader.UNIFORM_NAME.atlasOffset, atlasOffset);
                shader.LoadUniform(ImpostorShader.UNIFORM_NAME.worldPosition, entity.Position);
                shader.LoadUniform(ImpostorShader.UNIFORM_NAME.model, entity.ModelMatrix);
                shader.LoadUniform(ImpostorShader.UNIFORM_NAME.atlasSize, (float)settings.AtlasSize);
                shader.LoadUniform(ImpostorShader.UNIFORM_NAME.individualSize, (float)settings.IndividualSize);
                shader.LoadUniform(ImpostorShader.UNIFORM_NAME.aabbSizeModel, entity.ModelAABB.SphereRadius);
                shader.LoadUniform(ImpostorShader.UNIFORM_NAME.aabbCenterEntity, entity.AABB.Center);

                Gl.BindVertexArray(Point.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.DrawArrays(PrimitiveType.Points, 0, 1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }
            shader.Unbind();

            Gl.Enable(EnableCap.Blend);
        }

        public static void RenderLine(ColorShader shader, Camera camera, Vertex3f start, Vertex3f end, Vertex4f color, float thick)
        {            
            shader.Bind();

            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, color);

            Gl.BindVertexArray(Line.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(2);

            Vertex3f v = end - start;
            float size = v.Magnitude();
            Matrix4x4f scaled = Matrix4x4f.Scaled(size, 1, 1);
            EulerAngle angle = new EulerAngle(v, Vertex3f.UnitZ);
            Matrix4x4f rot = angle.Transform;
            Matrix4x4f fMat = rot * scaled;
            fMat[3, 0] = start.x;
            fMat[3, 1] = start.y;
            fMat[3, 2] = start.z;

            Gl.LineWidth(thick);
            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * fMat);
            Gl.DrawArrays(PrimitiveType.Lines, 0, Line.VertexCount);

            Gl.DisableVertexAttribArray(2);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            Gl.LineWidth(1.0f);
            shader.Unbind();
        }

        public static void RenderLocalAxis(ColorShader shader, List<Entity> entities, Camera camera, float thick = 3.0f)
        {
            OrbitCamera orbitCamera = camera as OrbitCamera;

            Gl.Disable(EnableCap.DepthTest);
            shader.Bind();
            Gl.LineWidth(thick);
            Gl.BindVertexArray(Renderer3d.Line.VAO);
            Gl.EnableVertexAttribArray(0);

            Matrix4x4f vp = orbitCamera.VPMatrix;

            foreach (Entity entity in entities)
            {
                Matrix4x4f mvp = vp * entity.OBB.ModelMatrix;

                shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, mvp);
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0, 1)); // red
                Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, mvp * Matrix4x4f.RotatedZ(90));
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0, 1)); // green
                Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, mvp * Matrix4x4f.RotatedY(-90));
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1, 1)); // blue
                Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);
            }

            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
            Gl.Enable(EnableCap.DepthTest);
            Gl.LineWidth(1.0f);
        }

        public static void RenderLocalAxis(StaticShader shader, Camera camera, float size, float thick, Matrix4x4f? localModel = null, bool isDepthTest = false)
        {
            if (localModel == null) localModel = Matrix4x4f.Identity;

            if (isDepthTest)
                Gl.Enable(EnableCap.DepthTest);
            else
                Gl.Disable(EnableCap.DepthTest);

            shader.Bind();

            shader.LoadUniform(StaticShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.view, camera.ViewMatrix);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.isTextured, false);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.isAttribColored, true);

            Gl.BindVertexArray(Axis.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(2);

            Matrix4x4f mat = (Matrix4x4f)localModel;
            Matrix4x4f scaled = Matrix4x4f.Scaled(0.3f * size, size, 0.5f * size);

            Gl.LineWidth(thick);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.model, mat * scaled);
            Gl.DrawArrays(PrimitiveType.Lines, 0, 6);

            Gl.DisableVertexAttribArray(2);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            shader.LoadUniform(StaticShader.UNIFORM_NAME.isAttribColored, false);
            shader.Unbind();

            if (isDepthTest)
                Gl.Disable(EnableCap.DepthTest);
            else
                Gl.Enable(EnableCap.DepthTest);
        }        

        public static void RenderTerrainOcc(ColorShader shader, TerrainOccluder3 occluder, Vertex4f color, Camera camera)
        {
            shader.Bind();
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, color);
            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * occluder.ModelMatrix);
            Gl.BindVertexArray(Renderer3d.Rect.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Rect.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
        }

        public static void RenderOBB(ColorShader shader, OBB obb, Vertex4f color, Camera camera)
        {
            shader.Bind();
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, color);
            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * obb.ModelMatrix);
            Gl.BindVertexArray(Renderer3d.Cube.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Cube.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
        }

        public static void RenderAABB(ColorShader shader, Matrix4x4f bindShape, Matrix4x4f jointMat, Matrix4x4f bindLocal, Matrix4x4f aabb, Vertex4f color, Camera camera)
        {
            shader.Bind();
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, color);
            Matrix4x4f model = bindShape * jointMat * Matrix4x4f.RotatedX(180) * Matrix4x4f.RotatedZ(180) * bindLocal * aabb;
            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * model);
            Gl.BindVertexArray(Cube.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Cube.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
        }

        public static void RenderAABB(ColorShader shader, AABB aabb, Vertex4f color, Camera camera)
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            shader.Bind();
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, color);
            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * aabb.ModelMatrix);
            Gl.BindVertexArray(Cube.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Cube.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();

            Gl.Disable(EnableCap.Blend);
        }


        public static void Render(ColorShader shader, Camera camera, WorldCoordinate worldCoordinate)
        {
            Gl.LineWidth(3.0f);
            Gl.Disable(EnableCap.DepthTest);
            shader.Bind();

            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix);

            foreach (RawModel3d rawModel in worldCoordinate.WorldAxis.Model)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0);

                // positive axis
                Gl.LineWidth(5.0f);
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0, 1)); // red
                Gl.DrawArrays(PrimitiveType.Lines, 0, 2);
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0, 1)); // green
                Gl.DrawArrays(PrimitiveType.Lines, 4, 2);
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1, 1)); // blue
                Gl.DrawArrays(PrimitiveType.Lines, 8, 2);

                // negative axis
                // <image url="$(ProjectDir)_PictureComment\lineStipplePattern.png" scale="0.8" />
                Gl.LineStipple(2, 0xAAAA);
                Gl.Enable(EnableCap.LineStipple);
                Gl.LineWidth(1.0f);
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0, 1)); // red
                Gl.DrawArrays(PrimitiveType.Lines, 2, 2);
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0, 1)); // green
                Gl.DrawArrays(PrimitiveType.Lines, 6, 2);
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1, 1)); // blue
                Gl.DrawArrays(PrimitiveType.Lines, 10, 2);
                Gl.Disable(EnableCap.LineStipple);

                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            shader.Unbind();
            Gl.Enable(EnableCap.DepthTest);
            Gl.LineWidth(1.0f);
        }

        public static void Render(StaticShader shader, ColorShader cShader, Entity entity, Camera camera, Vertex3f fogColor, Vertex4f fogPlane, float fogDensity)
        {
            PhysicalRenderEntity occlusionEntity = (entity is PhysicalRenderEntity) ? entity as PhysicalRenderEntity : null;

            OrbitCamera orbitCamera = camera as OrbitCamera;

            Gl.Disable(EnableCap.Blend);
            //Gl.BlendEquation(BlendEquationMode.FuncAdd);
            //Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusDstAlpha);
            
            shader.Bind();

            if (occlusionEntity != null)
            {
                entity = occlusionEntity.RenderEntity;
            }
            //if (entity is null) return;

            shader.LoadUniform(StaticShader.UNIFORM_NAME.fogColor, fogColor);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.fogDensity, fogDensity);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.fogPlane, fogPlane);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.isFogEnable, false);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.camPos, camera.Position);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.viewport_size, new Vertex2f(camera.Width, camera.Height));

            shader.LoadUniform(StaticShader.UNIFORM_NAME.proj, orbitCamera.ProjectiveMatrix);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.view, orbitCamera.ViewMatrix);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.model, entity.ModelMatrix);

            shader.LoadUniform(StaticShader.UNIFORM_NAME.mvp, orbitCamera.ProjectiveMatrix * orbitCamera.ViewMatrix * entity.ModelMatrix);

            // 모델을 그린다.
            foreach (RawModel3d rawModel in entity.Model)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);

                if (entity.IsTextured)
                {
                    shader.LoadUniform( StaticShader.UNIFORM_NAME.isTextured, true);
                    TexturedModel modelTextured = rawModel as TexturedModel;
                    if (modelTextured.Texture != null)
                    {
                        if (modelTextured.Texture.TextureType.HasFlag(Texture.TextureMapType.Diffuse))
                        {
                            shader.LoadTexture(StaticShader.UNIFORM_NAME.modelTexture, TextureUnit.Texture0, modelTextured.Texture.DiffuseMapID);
                        }
                        Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                        Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                    }
                }

                shader.LoadUniform(StaticShader.UNIFORM_NAME.color, entity.Material.Ambient);

                if (occlusionEntity is PhysicalRenderEntity)
                {
                    if (occlusionEntity.IsVisibleLOD)
                    {
                        if (occlusionEntity != null)
                        {
                            if (occlusionEntity.LOD == 0) shader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0, 1));
                            if (occlusionEntity.LOD == 1) shader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0, 1));
                            if (occlusionEntity.LOD == 2) shader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1, 1));
                        }
                    }
                }

                Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);

                Gl.DisableVertexAttribArray(2);
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            shader.Unbind();

            // axis를 그린다.
            if (entity.IsAxisVisible)
            {
                cShader.Bind();
                Gl.BindVertexArray(Renderer3d.Line.VAO);
                Gl.EnableVertexAttribArray(0);

                cShader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, occlusionEntity.ModelMatrix * Matrix4x4f.Scaled(occlusionEntity.OBB.Size.x, 1, 1));
                cShader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0, 1)); // red
                Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                cShader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, occlusionEntity.ModelMatrix * Matrix4x4f.RotatedZ(90) * Matrix4x4f.Scaled(occlusionEntity.OBB.Size.y, 1, 1));
                cShader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0, 1)); // green
                Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                cShader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, occlusionEntity.ModelMatrix * Matrix4x4f.RotatedY(-90) * Matrix4x4f.Scaled(occlusionEntity.OBB.Size.z, 1, 1));
                cShader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1, 1)); // blue

                Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
                cShader.Unbind();
            }
        }

        public static void RenderByTerrainTessellationShader(TerrainTessellationShader shader, Entity entity, Camera camera, Texture[] ground,
            Texture detailMap, bool isDetailMap, Vertex3f lightDirection, uint vegetationMap, float heightScale = 1.0f)
        {
            GlobalUniformBuffers.BindUBOsToShader(shader.ProgramID);

            shader.Bind();

            foreach (RawModel3d rawModel in entity.Model)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);

                TexturedModel modelTextured = rawModel as TexturedModel;
                shader.LoadUniform(TerrainTessellationShader.UNIFORM_NAME.gIsDetailMap, isDetailMap);
                shader.LoadUniform(TerrainTessellationShader.UNIFORM_NAME.gLightDir, lightDirection);
                shader.LoadUniform(TerrainTessellationShader.UNIFORM_NAME.heightScale, heightScale);

                shader.SetInt("gHeightMap", 0);
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2d, modelTextured.Texture.TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

                shader.SetInt("gDetailMap", 1);
                Gl.ActiveTexture(TextureUnit.Texture1);
                Gl.BindTexture(TextureTarget.Texture2d, detailMap==null? 0:detailMap.TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.NEAREST);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.NEAREST);

                for (int i = 0; i < 5; i++)
                {
                    shader.SetInt($"gTextureHeight{i}", i + 2);
                    Gl.ActiveTexture(TextureUnit.Texture2 + i);
                    Gl.BindTexture(TextureTarget.Texture2d, ground[i] == null ? 0 : ground[i].TextureID);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.MIRRORED_REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.MIRRORED_REPEAT);
                }

                shader.SetInt($"gVegetationMap",7);
                Gl.ActiveTexture(TextureUnit.Texture7);
                Gl.BindTexture(TextureTarget.Texture2d, vegetationMap);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

                shader.LoadUniform(TerrainTessellationShader.UNIFORM_NAME.color, entity.Material.Ambient);
                shader.LoadUniform(TerrainTessellationShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);
                shader.LoadUniform(TerrainTessellationShader.UNIFORM_NAME.view, camera.ViewMatrix);
                shader.LoadUniform(TerrainTessellationShader.UNIFORM_NAME.model, entity.ModelMatrix);
                shader.LoadUniform(TerrainTessellationShader.UNIFORM_NAME.camPos, camera.Position);

                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, rawModel.IBO);
                Gl.PatchParameter(PatchParameterName.PatchVertices, 4);
                Gl.DrawElements(PrimitiveType.Patches, rawModel.VertexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

                Gl.DisableVertexAttribArray(2);
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            shader.Unbind();
        }

        public static void RenderAtmosphereByRealTime(
            AtmosphericRenderShader shader,
            uint cloud3dTextureId,
            uint transmittanceLUTId,
            Camera camera,
            Vertex3f sunPosition,
            float earthRadiusMeter,
            float atmosphereRadiusMeter,
            float toneMappingFactor = 1.0f,
            Vertex3f? sunDiskColor = null,  // 태양 디스크 색상 (기본값 null)
            float sunDiskSize = 0.53f)      // 태양 디스크 크기 (기본값 약 0.53도)
        {

            GlobalUniformBuffers.BindUBOsToShader(shader.ProgramID);

            shader.Bind();
            shader.LoadTexture(AtmosphericRenderShader.UNIFORM_NAME.transmittanceLUT, TextureUnit.Texture0, transmittanceLUTId);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.R_e, earthRadiusMeter);      // 단위km
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.R_a, atmosphereRadiusMeter); // 단위km
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.beta_R, new Vertex3f(0.0058f, 0.0135f, 0.0331f));
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.beta_M, 0.0210f);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.H_R, 7.994f); // 단위km
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.H_M, 1.20f);  // 단위km
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.g, 0.65f);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.viewSamples, 8);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.I_sun, new Vertex3f(10.0f, 10.0f, 10.0f));
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.sunPos, sunPosition);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.toneMappingFactor, toneMappingFactor);

            // 구름 파라미터 설정
            shader.LoadTexture(AtmosphericRenderShader.UNIFORM_NAME.cloudTexture, TextureUnit.Texture1, cloud3dTextureId);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.enableClouds, 1.0f); // 구름 활성화
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.cloudCoverage, 0.65f); // 구름 커버리지
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.cloudDensity, 0.5f); // 구름 밀도
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.cloudSpeed, 0.03f); // 구름 속도
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.cloudHeight, 1.0f); // 구름 높이 (km)
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.cloudThickness, 2.0f); // 구름 두께 (km)
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.cloudBrightness, 1.2f); // 구름 밝기
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.cloudShadowStrength, 0.2f); // 그림자 강도

            // 태양 디스크 관련 유니폼 설정
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.sunDiskSize, sunDiskSize);
            if (sunDiskColor == null) sunDiskColor = new Vertex3f(1.0f, 0.95f, 0.9f); // 기본 태양 색상 (약간 황색)
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.sunDiskColor, (Vertex3f)sunDiskColor);

            // 기본 일출/일몰 색상 (붉은 오렌지색)
            Vertex3f sunsetColor = new Vertex3f(1.0f, 0.5f, 0.2f);

            // 유니폼 설정
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.sunsetFactor, 1.0f);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.sunsetColor, sunsetColor);

            // 대기 구를 카메라 중심으로 이동, 스케일은 R_a 기준, Fragment Shader에서는 km로 계산
            Matrix4x4f modelMatrix = Matrix4x4f.Translated(0, 0, camera.Position.z)
                * Matrix4x4f.Scaled(atmosphereRadiusMeter, atmosphereRadiusMeter, atmosphereRadiusMeter);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.model, modelMatrix);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.view, camera.ViewMatrix);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);
            shader.LoadUniform(AtmosphericRenderShader.UNIFORM_NAME.camPosMeter, camera.Position); 

            Gl.Disable(EnableCap.CullFace);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Gl.BindVertexArray(Renderer3d.Earth.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Earth.VertexCount);

            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            Gl.Enable(EnableCap.CullFace);

            shader.Unbind();
        }

        public static void Render(
            TerrainChunkShader shader,
            uint vao,
            uint ibo,
            List<AABB> aabbs, 
            Camera camera,
            Texture[] ground,
            Texture detailMap,
            Texture lowMap,
            Texture highMap,
            uint[] adjacentHeightMap,
            float blendFactor,
            bool isDetailMap, 
            Vertex3f lightDirection,
            uint vegetationMap,
            float chunkSize,
            float chunkSeperateCount,
            float heightScale)
        {
            shader.Bind();

            // 청크 유니폼 기본 영역
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.gIsDetailMap, isDetailMap);
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.gReversedLightDir, -lightDirection);
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.chunkSize, chunkSize);
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.chunkSeperateCount, chunkSeperateCount);
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.heightScale, heightScale);
                       
            shader.LoadTexture(TerrainChunkShader.UNIFORM_NAME.gDetailMap, TextureUnit.Texture1,
                detailMap == null ? 0 : detailMap.TextureID);

            for (int i = 0; i < 5; i++)
            {
                shader.SetInt($"gTextureHeight{i}", i + 2);
                Gl.ActiveTexture(TextureUnit.Texture2 + i);
                Gl.BindTexture(TextureTarget.Texture2d, ground[i] == null ? 0 : ground[i].TextureID);
            }

            // 여기서는 셰이더에 필요한 모든 텍스처를 등록
            shader.LoadTexture(TerrainChunkShader.UNIFORM_NAME.heightMapLowRes,
                TextureUnit.Texture7, lowMap.TextureID);

            shader.LoadTexture(TerrainChunkShader.UNIFORM_NAME.heightMapHighRes,
                TextureUnit.Texture8, highMap == null ? lowMap.TextureID : highMap.TextureID);

            // 인접한 높이맵
            for (int i = 0; i < 8; i++)
            {
                shader.SetInt($"adjacentHeightMap{i}", i + 10);
                Gl.ActiveTexture(TextureUnit.Texture0 + i + 10);
                Gl.BindTexture(TextureTarget.Texture2d, adjacentHeightMap[i]);
            }

            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.blendFactor, blendFactor);

            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.color, Vertex4f.One);
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.view, camera.ViewMatrix);
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.model, Matrix4x4f.Identity);
            shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.camPos, camera.Position);

            Gl.BindVertexArray(vao);
            Gl.EnableVertexAttribArray(0);
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            Gl.PatchParameter(PatchParameterName.PatchVertices, 4);

            foreach (AABB aabb in aabbs)
            {
                shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.chunkOffset, aabb.Center);
                shader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.chunkCoord, aabb.Index);
                Gl.DrawElements(PrimitiveType.Patches, 4, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }

            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            shader.Unbind();

        }

        public static void Render2(StaticShader shader, Entity entity, Camera camera)
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            shader.Bind();

            foreach (RawModel3d rawModel in entity.Model)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);

                if (entity.IsTextured)
                {
                    shader.LoadUniform(StaticShader.UNIFORM_NAME.isTextured, true);
                    TexturedModel modelTextured = rawModel as TexturedModel;
                    shader.LoadTexture(StaticShader.UNIFORM_NAME.modelTexture, TextureUnit.Texture0, modelTextured.Texture.TextureID);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                }

                shader.LoadUniform(StaticShader.UNIFORM_NAME.color, entity.Material.Ambient);
                shader.LoadUniform(StaticShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);
                shader.LoadUniform(StaticShader.UNIFORM_NAME.view, camera.ViewMatrix);
                shader.LoadUniform(StaticShader.UNIFORM_NAME.model, entity.ModelMatrix);

                Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);

                Gl.DisableVertexAttribArray(2);
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);

                if (entity.IsAxisVisible)
                {
                    Gl.BindVertexArray(Renderer3d.Line.VAO);
                    Gl.EnableVertexAttribArray(0);
                    shader.LoadUniform(StaticShader.UNIFORM_NAME.color, false);
                    shader.LoadUniform(StaticShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);
                    shader.LoadUniform(StaticShader.UNIFORM_NAME.view, camera.ViewMatrix);

                    shader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0, 1));
                    shader.LoadUniform(StaticShader.UNIFORM_NAME.model, entity.ModelMatrix * Matrix4x4f.Scaled(3, 3, 3));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, 2);

                    shader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0, 1));
                    shader.LoadUniform(StaticShader.UNIFORM_NAME.model, entity.ModelMatrix * Matrix4x4f.RotatedZ(90) * Matrix4x4f.Scaled(3, 3, 3));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, 2);

                    shader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1, 1));
                    shader.LoadUniform(StaticShader.UNIFORM_NAME.model, entity.ModelMatrix * Matrix4x4f.RotatedY(-90) * Matrix4x4f.Scaled(3, 3, 3));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, 2);

                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }
            }


            shader.Unbind();
        }

        public static void RenderPoint(ColorShader shader, Vertex3f point, Camera camera, Vertex4f color, float size = 0.1f)
        {
            shader.Bind();
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, color);
            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * Matrix4x4f.Translated(point.x, point.y, point.z) * Matrix4x4f.Scaled(size, size, size));
            Gl.BindVertexArray(Sphere.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Sphere.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
        }
    }
}
