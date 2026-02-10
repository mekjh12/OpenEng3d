namespace Common
{
    public static class StrRes
    {
        public readonly static string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";

        public readonly static string FONT_RESOURCES_FILENAME = @"\fonts\fontList.txt";


        public readonly static string[] TERRAIN_HEIGHT_FILENAMES = new string[5]
        {
            "water1.png",
            "rocky_terrain_02.png",
            "lowestTile.png",
            "HighTile.png",
            "highestTile.png"
        };

        public readonly static string[] TERRAIN_BIOM_ALPS_TEXTURES = new string[5]
        {
            @"alps\alps_lake_sand.png", // 수변 및 최저지대
            @"alps\alps_grass_meadow.png", // 알프스 초원
            @"alps\alps_rocky_forest.png", // 바위와 흙이 섞인 숲 지면
            @"alps\alps_cliff_face.png", // 가파른 암벽
            @"alps\alps_glacier_snow.png" // 만년설
        };

        public readonly static string[] TERRAIN_BIOM_TOLEDO_TEXTURES = new string[5]
        {
            @"toledo\toledo_tagus_riverbed.png",    // 0: 타호 강의 흙탕물/모래 바닥
            @"toledo\toledo_riverbank_green.png",   // 1: 강변의 덤불과 녹지
            @"toledo\toledo_arid_clay_hill.png",    // 2: 건조한 황토빛 점토 경사면
            @"toledo\toledo_fortress_cliff.png",    // 3: 도시를 받치고 있는 거친 암벽
            @"toledo\toledo_old_cobblestone.png"    // 4: 정상의 중세 돌바닥 (혹은 건조한 암석)
        };


        public readonly static string TERRAIN_ROOT_PATH = StrRes.PROJECT_PATH + @"\FormTools\bin\Debug\Res\Terrain\";

        public readonly static string TERRAIN_DETAILMAP_FILENAMES = @"\Res\Terrain\blend\detailMap.png";
    }
}
