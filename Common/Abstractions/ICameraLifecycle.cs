using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Abstractions
{
    public interface ICameraLifecycle
    {
        void Init(int width, int height);
        void Start();
        void Stop();
        void Resume();
        void ShutDown();
        void Update(int deltaTime);
    }
}
