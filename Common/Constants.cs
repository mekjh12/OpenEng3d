namespace Common
{
    public static class Constants
    {
        public const float Pi = 3.14159265359f;
        public const float TwoPi = 6.28318530718f;
        public const float HalfPi = 1.57079632679f;

        public const int MAX_INSTANCES = 100_000;       // 최대 인스턴스 수
        public const int MAX_BATCHES = 64;              // 최대 배치 수

        public const float TERRAIN_VERTICAL_SCALE = 408.32f;

        public static float CAMERA_MOVE_DELTA = 0.10f;
        public const int GROUND_FOG_MAX_INSTANCES = 5_000;

        public const int TERRAIN_TILE_SIZE = 1025;          // 1024+1
    }
}
