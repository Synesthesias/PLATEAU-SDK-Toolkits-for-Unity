namespace PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings
{
    public static class BuildingTypeExtensions
    {
        /// <summary>
        /// プロシージャルメッシュ生成に対応している建物タイプかどうか。
        /// </summary>
        public static bool SupportsProceduralGeneration(this BuildingType buildingType)
        {
            return buildingType != BuildingType.k_Unknown;
        }
    }
}
