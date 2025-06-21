using OpenGL;

namespace Ui2d
{
    public class Panel: Control
    {
        protected bool _isBorderd = false;
        protected float _borderWidth = 1.0f;
        protected Vertex3f _borderColor;

        public float BorderWidth
        {
            get => _borderWidth;
            set => _borderWidth = value;
        }

        public bool IsBorder
        {
            get => _isBorderd;
            set => _isBorderd = value;  
        }

        public Vertex3f BorderColor
        {
            get => _borderColor;
            set => _borderColor = value;
        }

        public Panel(string name) : base(name)
        {
            _isBorderd = false;
            _isDragDrop = false;
            _borderColor = new Vertex3f(1, 1, 1);
            _backColor = new Vertex3f(1, 1, 1);
        }

        public override void Shutdown()
        {

        }

        public override void Render(UIShader uiShader, FontRenderer fontRenderer)
        {
            if (!IsVisible) return;

            if (_isRenderable)
            {
                Gl.Enable(EnableCap.Blend);
                Gl.BlendEquation(BlendEquationMode.FuncAdd);
                Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                uiShader.Bind();

                uiShader.SetView(renderingPosition1.x, renderingPosition1.y, renderingWidth, renderingHeight);
                uiShader.LoadModelMatrix();

                uiShader.LoadEnableTexture(false);
                uiShader.LoadColor(new Vertex4f(_backColor.x, _backColor.y, _backColor.z, _alpha));
                uiShader.LoadTexcoordModelTransform(x: 0, y: 0, scaleX: 1, scaleY: 1);

                Gl.BindVertexArray(uiShader.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.DrawArrays(PrimitiveType.Triangles, 0, uiShader.VertexCount);
                Gl.DisableVertexAttribArray(0);
                Gl.DisableVertexAttribArray(1);
                Gl.BindVertexArray(0);

                // 만약 경계선 그리기가 참인 경우에 경계선을 그린다.
                if (_isBorderd)
                {
                    Gl.LineWidth(_borderWidth);
                    uiShader.LoadColor(new Vertex4f(_borderColor.x, _borderColor.y, _borderColor.z, 1.0f));
                    Gl.BindVertexArray(uiShader.VAO_LINE);
                    Gl.EnableVertexAttribArray(0);
                    uiShader.SetView(renderingPosition1.x, renderingPosition1.y, renderingWidth, renderingHeight);
                    uiShader.LoadModelMatrix();
                    Gl.DrawArrays(PrimitiveType.Lines, 0, 8);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                    Gl.LineWidth(1.0f);
                }

                uiShader.Unbind();
                Gl.Disable(EnableCap.Blend);
            }

            // traversal childs.
            if (_isRenderChild)
            {
                foreach (Control ctrl in _controls)
                {
                    if (ctrl.IsRendeable)
                    {
                        ctrl.Render(uiShader, fontRenderer);
                    }
                }
            }

            // top 컨트롤을 맨 위에 그린다.
            foreach (Control ctrl in _topControls) ctrl.Render(uiShader, fontRenderer);
        }
    }
}
