using System;
using System.IO;
using BTD_Mod_Helper.Api.Helpers;
using CommandLine;
using Il2CppAssets.Scripts.Models.GenericBehaviors;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Display;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppNinjaKiwi.Common.ResourceUtils;
namespace BTD_Mod_Helper.Api.Commands;

internal class ExportDisplayCommand : ModCommand<ExportCommand>
{
    public override string Command => "display";
    public override string Help => "Exports the textures and UnityDisplayNode information for the selected tower / other GUIDs";

    [Option('o', "open", Default = false, HelpText = "Also open the folder where pngs is exported")]
    public bool Open { get; set; }

    [Value(0, Default = "", MetaName = "GUID",
        HelpText = "Export the display for a specific GUID instead of the selected tower")]
    public string GUID { get; set; }

    public override bool Execute(ref string resultText)
    {
        if (string.IsNullOrWhiteSpace(GUID))
        {
            var tower = TowerSelectionMenu.instance.Exists()?.selectedTower?.tower;

            if (tower == null)
            {
                resultText = "No tower selected";
                return false;
            }

            Export(tower.GetUnityDisplayNode());
            resultText = "Export succeeded. See MelonLoader console for details.";
        }
        else
        {
            Game.instance.GetDisplayFactory().FindAndSetupPrototypeAsync(new PrefabReference(GUID), DisplayCategory.None,
                new Action<UnityDisplayNode>(Export));

            resultText = "Attempting to export display. See MelonLoader console for details.";
        }


        return true;
    }

    private void Export(UnityDisplayNode node)
    {
        foreach (var renderer in node.GetRenderers())
        {
            if (renderer?.material?.mainTexture == null) continue;

            var path = Path.Combine(FileIOHelper.sandboxRoot, renderer.name + ".png");
            renderer.material.mainTexture.TrySaveToPNG(path);

            ModHelper.Msg($"Saved {path}");
        }

        node.PrintInfo();

        if (Open)
        {
            ProcessHelper.OpenFolder(FileIOHelper.sandboxRoot);
        }
    }
}