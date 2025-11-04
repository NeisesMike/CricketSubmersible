using HarmonyLib;

namespace CricketVehicle
{
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.Start))]
        public static void StartPostfix(Player __instance)
        {
            // Setup build bot paths.
            // We have to do this at game-start time,
            // because the new objects we create are wiped on scene-change.
            UWE.CoroutineHost.StartCoroutine(VehicleFramework.VehicleBuilding.BuildBotManager.SetupBuildBotPaths(Cricket.storageContainer));
            return;
        }
    }
}
