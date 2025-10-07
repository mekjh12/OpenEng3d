using Camera3d;
using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;

namespace Animate
{
    public static class Renderer3d
    {
        #region 기본 3D 모델
        public static RawModel3d Point = Loader3d.LoadPoint(0, 0, 0);
        /// <summary>기본 라인 모델</summary>
        public static RawModel3d Line = Loader3d.LoadLine(0, 0, 0, 1, 0, 0);
        /// <summary>Z축 방향 라인 모델</summary>
        public static RawModel3d LineZ = Loader3d.LoadLine(0, 0, 0, 0, 0, 1);
        /// <summary>기본 큐브 모델</summary>
        public static RawModel3d Cube = Loader3d.LoadCube();
        /// <summary>기본 원뿔 모델 (4면체, 높이 3.0)</summary>
        public static RawModel3d Cone = Loader3d.LoadCone(4, 1.0f, 3.0f, false);
        /// <summary>기본 구체 모델 (반지름 1, 6면체)</summary>
        public static RawModel3d Sphere = Loader3d.LoadSphere(r: 1, piece: 6);
        /// <summary>지구본 모델 (반지름 1, 20면체)</summary>
        public static RawModel3d Earth = Loader3d.LoadSphere(r: 1, piece: 20);
        /// <summary>기본 사각형 모델</summary>
        public static RawModel3d Rect = Loader3d.LoadPlane();
        /// <summary>기본 쿼드 모델</summary>
        public static RawModel3d Quad = Loader3d.LoadQuad();
        /// <summary>기본 좌표축 모델</summary>
        public static RawModel3d Axis = Loader3d.LoadAxis(1);
        /// <summary>기본 실린더 모델 (12면체)</summary>
        public static RawModel3d Cylinder = Loader3d.LoadPrism(12, 1, 1, 1, Matrix4x4f.Identity);
        #endregion

        public static void RenderBone(AxisShader shader, ColorShader cshader, Camera camera,  IAnimActor actor, Bone bone,
            float lineWidth = 2.0f, float axisLength = 30.0f)
        {
            Matrix4x4f vp = camera.VPMatrix;
            int boneIndex = bone.Index;
            Matrix4x4f rootTransform = actor.Animator.RootTransforms[boneIndex];
            shader.RenderAxes(actor.ModelMatrix, rootTransform, vp, axisLength, lineWidth);

            Matrix4x4f finalMatrix = actor.ModelMatrix * rootTransform;
            Vertex3f p = finalMatrix.Position;
            Renderer3d.RenderPoint(cshader, p, camera, new Vertex4f(0, 1, 1, 1), 0.02f);
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

        // 1단계: 아이템 대신 간단한 큐브로 테스트
        public static void TestRenderSimpleCube(AnimateShader shader, Matrix4x4f model, Matrix4x4f vp,
            Matrix4x4f[] boneTransforms, int testBoneIndex)
        {
            shader.Bind();
            shader.LoadUniform(AnimateShader.UNIFORM_NAME.isSkinningEnabled, false);

            shader.LoadUniform(AnimateShader.UNIFORM_NAME.vp, vp);
            shader.LoadUniform(AnimateShader.UNIFORM_NAME.model, model);


            // 테스트용: 특정 본 위치에 간단한 형태 렌더링
            Matrix4x4f testScale = Matrix4x4f.Scaled(10.1f, 10.1f, 10.1f); // 작은 크기로 테스트


            Matrix4x4f testTransform = boneTransforms[52];
            shader.LoadPMatrix(testTransform * testScale);

            // 기본 큐브 렌더링 (텍스처 없이)
            Gl.BindVertexArray(Renderer3d.Cube.VAO);
            Gl.EnableVertexAttribArray(0);

            Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Cube.VertexCount);

            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
        }

        public static void RenderRigidBody(AnimateShader shader, Matrix4x4f model, Matrix4x4f vp, 
            List<ItemAttachment> items, Matrix4x4f[] boneTransforms)
        {
            // GC 압박을 줄이기 위하여 model과 vp는 GPU에서 계산한다.
            shader.Bind();
            shader.LoadUniform(AnimateShader.UNIFORM_NAME.vp, vp);
            shader.LoadUniform(AnimateShader.UNIFORM_NAME.model, model);
            shader.LoadUniform(AnimateShader.UNIFORM_NAME.isSkinningEnabled, false);

            foreach (ItemAttachment item in items)
            {
                if (item.BoneIndex < 0) continue;

                Matrix4x4f testTransform = boneTransforms[item.BoneIndex] * item.LocalTransform;
                shader.LoadPMatrix(testTransform);

                Gl.BindVertexArray(item.Model.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);

                if (item.Model is TexturedModel)
                {
                    if (item.Model.Texture != null)
                    {
                        shader.LoadTexture(AnimateShader.UNIFORM_NAME.diffuseMap, TextureUnit.Texture0, item.Model.Texture.TextureID);
                    }
                }

                if (item.Model.IsDrawElement)
                {
                    Gl.DrawElements(PrimitiveType.Triangles, item.Model.VertexCount, DrawElementsType.UnsignedInt, System.IntPtr.Zero);
                }
                else
                {
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, item.Model.VertexCount);
                }

                Gl.DisableVertexAttribArray(0);
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(2);
                Gl.BindVertexArray(0);
            }

            shader.Unbind();
        }

        public static void RenderSkinning(AnimateShader shader, Matrix4x4f model, Matrix4x4f vp, List<TexturedModel> texturedModels, Matrix4x4f[] finalAnimatedBoneMatrices)
        {
            shader.Bind();
            shader.LoadVPMatrix(vp);
            shader.LoadModelMatrix(model);
            shader.LoadIsSkinningEnabled(true);
            shader.LoadAllBoneMatrices(finalAnimatedBoneMatrices);

            for (int i = 0; i < texturedModels.Count; i++)
            {
                TexturedModel texturedModel = texturedModels[i];
                Gl.BindVertexArray(texturedModel.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);
                Gl.EnableVertexAttribArray(4);
                Gl.EnableVertexAttribArray(5);

                // 텍스처 바인딩을 드로우 직전으로 이동
                if (texturedModel.Texture != null)
                {
                    Gl.ActiveTexture(TextureUnit.Texture0);
                    Gl.BindTexture(TextureTarget.Texture2d, texturedModel.Texture.TextureID);
                }

                // 즉시 드로우
                if (texturedModel.IsDrawElement)
                {
                    Gl.DrawElements(PrimitiveType.Triangles, texturedModel.VertexCount, DrawElementsType.UnsignedInt, System.IntPtr.Zero);
                }
                else
                {
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, texturedModel.VertexCount);
                }

                Gl.DisableVertexAttribArray(0);
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(2);
                Gl.DisableVertexAttribArray(4);
                Gl.DisableVertexAttribArray(5);
                Gl.BindVertexArray(0);
            }

            shader.Unbind();
        }

        public static void RenderLocalAxis(ColorShader shader, Matrix4x4f mat, Matrix4x4f vp, float thick = 1.0f)
        {
            Gl.Disable(EnableCap.DepthTest);
            shader.Bind();
            Gl.LineWidth(thick);
            Gl.BindVertexArray(Renderer3d.Line.VAO);
            Gl.EnableVertexAttribArray(0);

            Matrix4x4f mvp = vp * mat * Matrix4x4f.Scaled(10, 10, 10);

            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, mvp);
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0, 1)); // red
            Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, mvp * Matrix4x4f.RotatedZ(90));
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0, 1)); // green
            Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, mvp * Matrix4x4f.RotatedY(-90));
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1, 1)); // blue
            Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
            Gl.Enable(EnableCap.DepthTest);
            Gl.LineWidth(1.0f);
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
            Matrix4x4f scaled = Matrix4x4f.Scaled(10.3f * size, size, 10.5f * size);

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

    }
}
