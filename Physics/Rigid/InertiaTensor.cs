using OpenGL;

namespace Physics
{
    public static class InertiaTensor
    {
        public static Matrix3x3f Cube(float mass, float dx = 1.0f, float dy = 1.0f, float dz = 1.0f)
        {
            Matrix3x3f imat = Matrix3x3f.Identity;
            float ddx = dx * dx;
            float ddy = dy * dy;
            float ddz = dz * dz;
            imat[0, 0] = mass * (1.0f / 12.0f) * (ddy + ddz);
            imat[1, 1] = mass * (1.0f / 12.0f) * (ddx + ddz);
            imat[2, 2] = mass * (1.0f / 12.0f) * (ddx + ddy);
            return imat;
        }

        public static Matrix3x3f Cylinder(float mass, float dr = 1.0f, float dh = 1.0f)
        {
            Matrix3x3f imat = Matrix3x3f.Identity;
            float ddr = dr * dr;
            float ddh = dh * dh;
            imat[0, 0] = mass * (1.0f / 12.0f) * (ddh) + mass * (1.0f / 4.0f) * (ddr);
            imat[1, 1] = mass * (1.0f / 12.0f) * (ddh) + mass * (1.0f / 4.0f) * (ddr);
            imat[2, 2] = mass * (1.0f / 2.0f) * (ddr);
            return imat;
        }

        public static Matrix3x3f Sphere(float mass, float dr = 1.0f)
        {
            Matrix3x3f imat = Matrix3x3f.Identity;
            float ddr = dr * dr;
            imat[0, 0] = mass * (2.0f / 5.0f) * (ddr);
            imat[1, 1] = mass * (2.0f / 5.0f) * (ddr);
            imat[2, 2] = mass * (2.0f / 5.0f) * (ddr);
            return imat;
        }
    }
}
