﻿using System.Collections;
using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.ModMenu;
using BTD_Mod_Helper.Api.ModOptions;
using Il2CppAssets.Scripts.Unity.UI_New.Settings;
using UnityEngine;
using Object = Il2CppSystem.Object;
namespace BTD_Mod_Helper.UI.Menus;

internal class ModSettingsMenu : ModGameMenu<HotkeysScreen>
{
    public static BloonsTD6Mod BloonsTD6Mod { get; private set; }

    private Animator animator;

    private CanvasGroup canvasGroup;

    private ModHelperScrollPanel scrollPanel;

    public override bool OnMenuOpened(Object data)
    {
        var gameObject = GameMenu.gameObject;
        gameObject.DestroyAllChildren();
        GameMenu.saved = true;

        CommonForegroundHeader.SetText(BloonsTD6Mod.Info.Name);

        scrollPanel = gameObject.AddModHelperScrollPanel(new Info("ScrollPanel", InfoPreset.FillParent),
            RectTransform.Axis.Vertical, null, 150, 300);

        animator = scrollPanel.AddComponent<Animator>();
        animator.runtimeAnimatorController = Animations.PopupAnim;
        animator.speed = .65f;
        animator.Rebind();

        canvasGroup = scrollPanel.AddComponent<CanvasGroup>();

        MelonCoroutines.Start(CreateMenuContent());

        return true;
    }

    public IEnumerator CreateMenuContent()
    {
        foreach (var (category, modSettings) in BloonsTD6Mod.ModSettings.Values
                     .GroupBy(setting => setting.category)
                     .OrderBy(kvp => kvp.Key?.order ?? 0))
        {
            var content = scrollPanel.ScrollContent;
            if (category != null)
            {
                var categoryOption = category.Create();
                yield return null;

                if (Closing) yield break;

                scrollPanel.AddScrollContent(categoryOption);
                content = categoryOption.CategoryContent;
                yield return null;

                if (Closing) yield break;
            }

            foreach (var modSetting in modSettings)
            {
                var modHelperOption = modSetting.CreateComponent();
                yield return null;

                if (Closing) yield break;

                modSetting.currentOption = modHelperOption;
                if (modHelperOption.ResetButton.gameObject.active)
                {
                    modHelperOption.BottomRow.AddPanel(new Info("Empty", size: ModHelperOption.ResetSize));
                }

                content.Add(modHelperOption);

                yield return null;

                if (Closing) yield break;
            }
        }
    }

    public override void OnMenuUpdate()
    {
        if (Closing)
        {
            canvasGroup.alpha -= .07f;
        }
    }

    public override void OnMenuClosed()
    {
        animator.Play("PopupSlideOut");
        ModSettingsHandler.SaveModSettings(BloonsTD6Mod);
    }

    public static void Open(BloonsTD6Mod BloonsTD6Mod)
    {
        BloonsTD6Mod = BloonsTD6Mod;
        Open<ModSettingsMenu>();
    }
}
