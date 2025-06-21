using OpenGL;
using System;
using System.Collections.Generic;

namespace Ui2d
{
    partial class UIEngine
    {
        private static event Action<int, int> _designInit;

        public static Action<int, int> DesignInit
        {
            get => _designInit;
            set => _designInit = value;
        }

        private void Intialize()
        {
            _rootPannel = new Panel($"{_name}_RootPannel")
            {
                IsRendeable = true,
                IsVisible = true,
                Alpha = 0,
                Location = new Vertex2f(0.0f, 0.0f),
                Width = 1.0f,
                Height = 1.0f,
            };
            RegistryControl(_rootPannel);
        }
    }
}
