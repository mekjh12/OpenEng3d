using OpenGL;
using Ui2d;
using ZetaExt;

namespace GlWindow
{
    public partial class GlControl3
    {
        // ==================================================================================
        //                              UI 컨트롤 추가 메서드
        // ==================================================================================

        /// <summary>
        /// UI2d 메인에 컨트롤 추가
        /// </summary>
        public void AddControl2d(Ui2d.Control control)
        {
            UIEngine.AddControl("mainUI", control);
        }

        /// <summary>
        /// 체크박스 추가
        /// </summary>
        public void AddCheckBar(string name, string text, string formName = "sysInfo",
            Vertex3f? foreColor = null, float fontSize = 1.0f, Ui2d.FontFamily fontFamily = null,
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
            Ui2d.Control adjoint = null, bool value = false)
        {
            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            if (foreColor == null) foreColor = new Vertex3f(1, 1, 1);

            SimpCheckbox chkBox = new SimpCheckbox(name, fontFamily)
            {
                ForeColor = (Vertex3f)foreColor,
                FontSize = fontSize,
                Text = text,
                Margin = 0.5f,
                Checked = value,
                Alpha = 0.0f,
                MouseDown = (o, mx, my) => { }
            };
            AddControlWith(chkBox, adjoint, align, formName);
        }

        /// <summary>
        /// 컨트롤을 지정된 위치에 추가
        /// </summary>
        public void AddControlWith(Ui2d.Control ctrl, Ui2d.Control adjoint,
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START, string formName = "sysInfo")
        {
            if (_lastControl != null)
            {
                ctrl.AdjontControl = _lastControl;
            }

            if (adjoint != null)
            {
                ctrl.AdjontControl = adjoint;
            }

            if (_lastControl == CLabel("fps"))
            {
                ctrl.Align = Ui2d.Control.CONTROL_ALIGN.ROOT_TL;
                ctrl.Margin = 0.5f;
            }
            else
            {
                ctrl.Align = align;
            }

            UIEngine.AddControl(formName, ctrl);
            _lastControl = ctrl;
        }

        /// <summary>
        /// 체크리스트 추가
        /// </summary>
        public SimpCheckList AddCheckList(string name, string text, string[] items,
            string formName = "sysInfo",
            Vertex3f? foreColor = null, float fontSize = 1.0f, Ui2d.FontFamily fontFamily = null,
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
            Ui2d.Control adjoint = null, bool value = false)
        {
            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            if (foreColor == null) foreColor = new Vertex3f(1, 1, 1);

            SimpCheckList chkList = new SimpCheckList(name, fontFamily)
            {
                ForeColor = (Vertex3f)foreColor,
                Alpha = 0.6f,
                Margin = 0.01f,
                FontSize = fontSize,
                Items = items,
            };
            AddControlWith(chkList, adjoint, align, formName);

            _lastControl = chkList;
            return chkList;
        }

        /// <summary>
        /// 라벨 추가
        /// </summary>
        public Ui2d.Label AddLabel(string name, string text, string formName = "sysInfo",
            Vertex3f? foreColor = null, float fontSize = 1.0f, Ui2d.FontFamily fontFamily = null,
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
            Ui2d.Control adjoint = null, Vertex2f? location = null)
        {
            if (location == null) location = Vertex2f.Zero;
            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            if (foreColor == null) foreColor = new Vertex3f(1, 1, 1);

            Ui2d.Label lbl = new Ui2d.Label(name, fontFamily)
            {
                ForeColor = (Vertex3f)foreColor,
                FontSize = fontSize,
                Text = text,
                Alpha = 0.0f,
                Margin = 0.05f,
                BackColor = Vertex3f.UnitY,
                Location = (Vertex2f)location,
                MouseDown = (o, mx, my) => { }
            };

            AddControlWith(lbl, adjoint, align, formName);
            return lbl;
        }

        /// <summary>
        /// 값 조절 바 추가
        /// </summary>
        public SimpHValueBar AddValueBar(string name, string formName = "sysInfo",
            Vertex3f? foreColor = null, Vertex3f? backColor = null,
            float fontSize = 1.0f, Ui2d.FontFamily fontFamily = null,
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
            Ui2d.Control adjoint = null, float width = 0.2f
            , float maxValue = 1.0f, float minValue = 0.0f, float value = 0.5f, float stepValue = 0.1f)
        {
            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            if (foreColor == null) foreColor = new Vertex3f(1, 1, 0);
            if (backColor == null) backColor = new Vertex3f(0, 0, 1);

            SimpHValueBar vbar = new SimpHValueBar(name)
            {
                ForeColor = (Vertex3f)foreColor,
                ValueColor = (Vertex3f)foreColor,
                BackColor = (Vertex3f)backColor,
                FontSize = fontSize,
                MaxValue = maxValue,
                MinValue = minValue,
                StepValue = stepValue,
                Margin = 0.2f,
                Value = value,
                Height = 0.1f * width,
                Width = width,
                Round = 3,
                IsIniWritable = true,
            };

            vbar.MouseWheel += (o, delta) =>
            {
                if (o.IsIniWritable)
                {
                    IniFile.WritePrivateProfileString(formName, name, (o as SimpHValueBar).Value);
                }
            };

            AddControlWith(vbar, adjoint, align, formName);
            return vbar;
        }

        // ==================================================================================
        //                              UI 컨트롤 가져오기 메서드
        // ==================================================================================

        /// <summary>
        /// 라벨 컨트롤 가져오기
        /// </summary>
        public Ui2d.Label CLabel(string name)
        {
            return (Ui2d.Label)UIEngine.Controls(name);
        }

        /// <summary>
        /// 체크리스트 컨트롤 가져오기
        /// </summary>
        public Ui2d.SimpCheckList CheckList(string name)
        {
            return (Ui2d.SimpCheckList)UIEngine.Controls(name);
        }

        /// <summary>
        /// 값 조절 바 컨트롤 가져오기
        /// </summary>
        public Ui2d.SimpHValueBar SimpHValueBar(string name)
        {
            return (SimpHValueBar)UIEngine.Controls(name);
        }

        /// <summary>
        /// 체크박스 컨트롤 가져오기
        /// </summary>
        public Ui2d.SimpCheckbox SimpCheckBox(string name)
        {
            return (SimpCheckbox)UIEngine.Controls(name);
        }

        /// <summary>
        /// 컨트롤 가져오기
        /// </summary>
        public Ui2d.Control Ctrl(string name)
        {
            return UIEngine.Controls(name);
        }
    }
}