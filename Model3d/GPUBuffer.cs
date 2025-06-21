using OpenGL;
using System.Collections.Generic;

namespace Model3d
{
    /// <summary>
    /// Graphic Memory Manage. (VAO, VBO)
    /// </summary>
    public class GPUBuffer
    {
        //-------------------------vao,  vbo list----------------------------------------------
        private static Dictionary<uint, List<uint>> _vbos = new Dictionary<uint, List<uint>>();

        public static void Add(uint vao, uint vbo)
        {
            if (!_vbos.ContainsKey(vao))
            {
                List<uint> list = new List<uint>();
                list.Add(vbo);
                _vbos.Add(vao, list);
            }
            else
            {
                List<uint> list = _vbos[vao];
                list.Add(vbo);
            }
        }

        public static void CleanAt(uint vao)
        {
            Gl.DeleteBuffers(vao);
            List<uint> vbos = _vbos[vao];
            Gl.DeleteVertexArrays(vbos.ToArray());
            _vbos.Remove(vao);
        }

        public static void Clean()
        {
            foreach (KeyValuePair<uint, List<uint>> item in _vbos)
            {
                _vbos.Remove(item.Key);
                Gl.DeleteTextures(item.Value.ToArray());
            }
        }
    }
}
