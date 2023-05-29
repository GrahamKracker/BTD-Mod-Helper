using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Towers;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation.Input;
using Il2CppAssets.Scripts.Unity;
namespace BTD_Mod_Helper.Patches.Towers;

[HarmonyPatch(typeof(TowerInventory), nameof(TowerInventory.Init))]
internal static class TowerInventory_Init
{
    [HarmonyPrefix]
    private static void Prefix(TowerInventory __instance,
        ref Il2CppSystem.Collections.Generic.IEnumerable<TowerDetailsModel> allTowersInTheGame)
    {
        var list = allTowersInTheGame.ToList();
        var unref_allTowersInTheGame = (IEnumerable<TowerDetailsModel>) list;

        ModHelper.PerformHook(mod => mod.PreTowerInventoryInit(__instance, ref unref_allTowersInTheGame));

        var allTowers = Game.instance.model.towerSet.ToList();

        foreach (var modTower in ModContent.GetContent<ModTower>())
        {
            var towerModel = Game.instance.model.GetTowerWithName(modTower.Id);

            if (modTower.DontAddToShop && !towerModel.isSubTower)
            {
                var index = Math.Clamp(modTower.GetTowerIndex(allTowers), 0, allTowers.Count);
                list.Insert(index, new ShopTowerDetailsModel(modTower.Id, index, 5, 5, 5, modTower.ShopTowerCount, 0));
            }
        }

        allTowersInTheGame = list.ToIl2CppReferenceArray()
            .Cast<Il2CppSystem.Collections.Generic.IEnumerable<TowerDetailsModel>>();
    }

    [HarmonyPostfix]
    private static void Postfix(TowerInventory __instance, Il2CppSystem.Collections.Generic.IEnumerable<TowerDetailsModel> allTowersInTheGame)
    {
        var list = allTowersInTheGame.ToList();
        ModHelper.PerformHook(mod => mod.OnTowerInventoryInit(__instance, list));
    }
}