using System;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormTest : Form
    {
        public FormTest()
        {
            InitializeComponent();
        }

        private void FormTest_Load(object sender, EventArgs e)
        {
            MemoryProfiler.StartFrameMonitoring();
        }

        private void glControl1_Render(object sender, OpenGL.GlControlEventArgs e)
        {
            MemoryProfiler.CheckFrameGC();
        }
    }
}
