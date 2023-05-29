﻿using System.Runtime.InteropServices;
using BTD_Mod_Helper.Api.Coop;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Map;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Mods;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Simulation.Input;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Simulation.Towers.Projectiles;
using Il2CppAssets.Scripts.Simulation.Towers.Weapons;
using Il2CppAssets.Scripts.Unity.Audio;
using Il2CppAssets.Scripts.Unity.Display;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Utils;
using Il2CppNinjaKiwi.NKMulti;
using UnityEngine.UI;
using ObsoleteAttribute = System.ObsoleteAttribute;
using System.Collections.Generic;
using BTD_Mod_Helper.Api;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Models.Rounds;
using Il2CppAssets.Scripts.Simulation.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Simulation.Track;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using Il2CppAssets.Scripts.Unity.UI_New.Pause;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using Removeable = Il2CppAssets.Scripts.Simulation.Track.Removeable;
using System.IO;
using System.Linq;
using BTD_Mod_Helper.Api.ModOptions;
using Path = System.IO.Path;
using Type = System.Type;

namespace BTD_Mod_Helper;

/// <summary>
/// Extend this Class instead of MelonMod to gain access to dozens of easy to use built-in hooks
/// </summary>
public abstract class BloonsTD6Mod : MelonMod, IModContent
{
    private List<ModContent> content;

    /// <summary>
    /// All ModContent in ths mod
    /// </summary>
    public IReadOnlyList<ModContent> Content
    {
        get => content;
        internal set => content = value as List<ModContent>;
    }

    /// <summary>
    /// The settings in this mod organized by name
    /// </summary>
    public Dictionary<string, ModSetting> ModSettings { get; } = new();

    /// <summary>
    /// The embedded resources (textures) of this mod
    /// </summary>
    public Dictionary<string, byte[]> Resources { get; internal set; }

    /// <summary>
    /// Audio clips for the embedded sounds in this mod
    /// </summary>
    public Dictionary<string, AudioClip> AudioClips { get; internal set; }

    /// <summary>
    /// The prefix used for the IDs of towers, upgrades, etc for this mod to prevent conflicts with other mods
    /// </summary>
    public virtual string IDPrefix => this.GetAssembly().GetName().Name + "-";

    /// <summary>
    /// Setting this to true will prevent your BloonsTD6Mod hooks from executing if the player could get flagged for using mods at that time.
    /// <br/> 
    /// For example, using mods in public co-op
    /// </summary>
    public virtual bool CheatMod => true;

    internal string SettingsFilePath
    {
        get
        {
            var oldPath = Path.Combine(ModHelper.ModSettingsDirectory, $"{Info.Name}.json");
            var newPath = Path.Combine(ModHelper.ModSettingsDirectory, $"{this.GetAssembly().GetName().Name}.json");
            return File.Exists(oldPath) ? oldPath : newPath;
        }
    }

    private ModLoadTask loadContentTask;

    internal ModLoadTask LoadContentTask => loadContentTask ??= new ModContentTask {mod = this};

    /// <summary>
    /// The path that this mod would most likely be at in the Mod Sources folder
    /// </summary>
    public string ModSourcesPath =>
        Path.Combine(ModHelper.ModSourcesDirectory, this.GetAssembly().GetName().Name!);

    internal readonly HashSet<Type> GotModTooSoon = new();

    /// <summary>
    /// <see href="https://github.com/gurrenm3/BTD-Mod-Helper/wiki/%5B3.0%5D-Appearing-in-the-Mod-Browser-%28ModHelperData%29"/>
    /// </summary>
    [Obsolete("Switch to using ModHelperData")]
    public virtual string GithubReleaseURL => "";

    /// <summary>
    /// <see href="https://github.com/gurrenm3/BTD-Mod-Helper/wiki/%5B3.0%5D-Appearing-in-the-Mod-Browser-%28ModHelperData%29"/>
    /// </summary>
    [Obsolete("Switch to using ModHelperData")]
    public virtual string MelonInfoCsURL => "";

    /// <summary>
    /// <see href="https://github.com/gurrenm3/BTD-Mod-Helper/wiki/%5B3.0%5D-Appearing-in-the-Mod-Browser-%28ModHelperData%29"/>
    /// </summary>
    [Obsolete("Switch to using ModHelperData (wiki page)")]
    public virtual string LatestURL => "";

    /// <summary>
    /// Allows you to define ways for other mods to interact with this mod. Other mods could do:
    /// <code>
    /// ModHelper.GetMod("YourModName")?.Call("YourOperationName", ...);
    /// </code>
    /// to execute functionality here.
    /// <br/>
    /// </summary>
    /// <param name="operation">A string for the name of the operation that another mods wants to call</param>
    /// <param name="parameters">The parameters that another mod has provided</param>
    /// <returns>A possible result of this call</returns>
    public virtual object Call(string operation, params object[] parameters)
    {
        return null;
    }

    /// <summary>
    /// Manually adds new ModContent to the mod. Does not directly call <see cref="ModContent.Load()"/> or
    /// <see cref="ModContent.Register"/>, but the latter will still end up being called if this is added before the
    /// Registration phase.
    /// </summary>
    /// <param name="modContent"></param>
    public void AddContent(ModContent modContent)
    {
        modContent.mod = this;
        content.Add(modContent);
        ModContentInstances.AddInstance(modContent.GetType(), modContent);
    }

    /// <summary>
    /// Manually adds multiple new ModContent to the mod. Does not directly call <see cref="ModContent.Load()"/> or
    /// <see cref="ModContent.Register"/>, but the latter will still end up being called if this is added before the
    /// Registration phase.
    /// </summary>
    public void AddContent(IEnumerable<ModContent> modContents)
    {
        var contents = modContents.ToList();
        foreach (var modContent in contents)
        {
            modContent.mod = this;
            ModContentInstances.AddInstance(modContent.GetType(), modContent);
        }
        content.AddRange(contents);
    }
    public readonly List<string> loadErrors = new();

    /// <summary>
    /// Signifies that the game shouldn't crash / the mod shouldn't stop loading if one of its patches fails
    /// </summary>
    public virtual bool OptionalPatches => true;


    /// <summary>
    /// Lets the ModHelper control patching, allowing for individual patches to fail without the entire mod getting
    /// unloaded. 
    /// </summary>
    internal bool modHelperPatchAll;
    
    #region ModHelper Overrides

    /// <inheritdoc />
    public sealed override void OnEarlyInitializeMelon()
    {
        ModContentInstances.AddInstance(GetType(), this);

        // If they haven't set OptionalPatches to false and haven't already signified they have their own patching plan
        // by using HarmonyDontPatchAll themselves...
        if (OptionalPatches && !MelonAssembly.HarmonyDontPatchAll)
        {
            typeof(MelonAssembly)
                .GetProperty(nameof(MelonAssembly.HarmonyDontPatchAll))!
                .GetSetMethod(true)!
                .Invoke(MelonAssembly, new object[] {true});

            modHelperPatchAll = true;
        }

        OnEarlyInitialize();
    }

    /// <inheritdoc />
    public sealed override void OnInitializeMelon()
    {
        if (modHelperPatchAll)
        {
            AccessTools.GetTypesFromAssembly(this.GetAssembly()).Do(type =>
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(type).Patch();
                }
                catch (System.Exception e)
                {
                    MelonLogger.Warning(
                        $"Failed to apply {Info.Name} patch(es) in {type.Name}: \"{e.InnerException?.Message ?? e.Message}\" " +
                        $"The mod might not function correctly. This needs to be fixed by {Info.Author}");

                    loadErrors.Add($"Failed to apply patch(es) in {type.Name}");

                    /*if (type == typeof(Task_EnumerateAction) || type == typeof(Main_GetInitialLoadTasks))
                    {
                        ModHelper.FallbackToOldLoading = true;
                        ModHelper.Msg("Falling back to old loading method");
                    }*/
                }
            });
        }

        if (GotModTooSoon.Contains(GetType()) && IDPrefix != this.GetAssembly().GetName().Name + "-")
        {
            // Happens when trying to get a custom embedded resource during the static constructor phase
            LoggerInstance.Warning("Tried to get mod id prefix too soon, used default value at least once");
        }

        OnApplicationStart();
        OnInitialize();
    }

    #endregion

    #region Networking Hooks

    /// <summary>
    /// Executed once the user has connected to a game session.
    /// Note: Only invoked if <see cref="BloonsTD6Mod.CheatMod"/> == true.
    /// </summary>
    /// <param name="nkGi">The interface used to interact with the game.</param>
    [Obsolete("Currently disabled due to instability")]
    public virtual void OnConnected(NKMultiGameInterface nkGi)
    {
    }

    /// <summary>
    /// Executed once the user has tried to connect to a server, but failed to do so.
    /// Note: Only invoked if <see cref="BloonsTD6Mod.CheatMod"/> == true.
    /// </summary>
    /// <param name="nkGi">The interface used to interact with the game.</param>
    [Obsolete("Currently disabled due to instability")]
    public virtual void OnConnectFail(NKMultiGameInterface nkGi)
    {
    }

    /// <summary>
    /// Executed once the player has disconnected from a server.
    /// Note: Only invoked if <see cref="BloonsTD6Mod.CheatMod"/> == true.
    /// </summary>
    /// <param name="nkGi">The interface used to interact with the game.</param>
    public virtual void OnDisconnected(NKMultiGameInterface nkGi)
    {
    }

    /// <summary>
    /// Executed when a new client has joined the game session.
    /// Note: Only invoked if <see cref="BloonsTD6Mod.CheatMod"/> == true.
    /// </summary>
    /// <param name="nkGi">The interface used to interact with the game.</param>
    /// <param name="peerId">Index of the peer in question.</param>
    [Obsolete("Currently disabled due to instability")]
    public virtual void OnPeerConnected(NKMultiGameInterface nkGi, int peerId)
    {
    }

    /// <summary>
    /// Executed when a new client has left the game session.
    /// Note: Only invoked if <see cref="BloonsTD6Mod.CheatMod"/> == true.
    /// </summary>
    /// <param name="nkGi">The interface used to interact with the game.</param>
    /// <param name="peerId">Index of the peer in question.</param>
    [Obsolete("Currently disabled due to instability")]
    public virtual void OnPeerDisconnected(NKMultiGameInterface nkGi, int peerId)
    {
    }

    /// <summary>
    /// Acts on a Network message that's been sent to the client
    /// <br/>
    /// Use <see cref="MessageUtils.ReadMessage{T}(Message)"/> to read back the message you sent.
    /// <br/>
    /// If this is one of your messages and you're consuming and acting on it, return true.
    /// Otherwise, return false. Seriously.
    /// Note: Only invoked if <see cref="BloonsTD6Mod.CheatMod"/> == true.
    /// </summary>
    public virtual bool ActOnMessage(Message message)
    {
        return false;
    }

    #endregion

    #region Loading Hooks

    /// <summary>
    /// Called when a map model is loaded by the game. Equivelant to MapLoader.Load.
    /// </summary>
    /// <param name="mapModel">The map that was just loaded. It is passed by reference to allow for modifications.</param>
    [Obsolete("No longer implemented.")]
    public virtual void OnMapModelLoaded(ref MapModel mapModel)
    {
    }


    /// <summary>
    /// Called when the player's ProfileModel is loaded.
    /// <br/>
    /// Equivalent to a HarmonyPostFix on ProfileModel.Validate
    /// </summary>
    public virtual void OnProfileLoaded(ProfileModel result)
    {
    }

    /// <summary>
    /// Perform actions on a profile right before the Mod Helper cleans it. If you see that the Mod Helper cleans
    /// past profile data from your mod on startup, it should be removed here.
    /// </summary>
    public virtual void PreCleanProfile(ProfileModel profile)
    {
    }

    /// <summary>
    /// If you removed any data in PreCleanProfile, you may want to add it back here, or just call <see cref="OnProfileLoaded"/>
    /// again on the profile.
    /// </summary>
    public virtual void PostCleanProfile(ProfileModel profile)
    {
    }

    /// <summary>
    /// Called when InGame.instance.UnityToSimulation.Simulation is not null
    /// </summary>
    public virtual void OnInGameLoaded(InGame inGame)
    {
    }

    /// <summary>
    /// Called when Game.instance.model is not null
    /// </summary>
    public virtual void OnGameModelLoaded(GameModel model)
    {
    }

    /// <summary>
    /// Called when a new GameModel is created, aka when things like Monkey Knowledge are applied to towers
    /// <br/>
    /// Equivalent to a HarmonyPostFix on GameModel.CreatedModded
    /// </summary>
    public virtual void OnNewGameModel(GameModel result)
    {
    }

    /// <summary>
    /// Called when the TowerInventory is initialized for a game
    /// </summary>
    /// <param name="towerInventory"></param>
    /// <param name="allTowersInTheGame"></param>
    [Obsolete("No longer implemented.")]
    public virtual void OnTowerInventoryInitialized(TowerInventory towerInventory,
        List<TowerDetailsModel> allTowersInTheGame)
    {
    }

    /// <summary>
    /// Called when a new GameModel is created, aka when things like Monkey Knowledge are applied to towers
    /// <br/>
    /// Equivalent to a HarmonyPostFix on GameModel.CreatedModded
    /// </summary>
    public virtual void OnNewGameModel(GameModel result, List<ModModel> mods)
    {
    }

    /// <summary>
    /// Called when a new GameModel is created, including the map
    /// <br/>
    /// Equivalent to a HarmonyPostFix on GameModel.CreatedModded
    /// </summary>
    public virtual void OnNewGameModel(GameModel result, MapModel map)
    {
    }

    /// <summary>
    /// Called when a display is being loaded such as a towers 3d model
    /// <br/>
    /// Equivalent to a HarmonyPostFix on GameModel.CreatedModded
    /// </summary>
    [Obsolete("No longer implemented.")]
    public virtual void OnModelLoaded(Factory factory, string ModelToLoad, System.Action<UnityDisplayNode> action)
    {
    }

    /// <summary>
    /// Called when the games audioFactory is loaded
    /// <br/>
    /// Equivalent to a HarmonyPostFix on AudioFactory.Start
    /// </summary>
    public virtual void OnAudioFactoryStart(AudioFactory audioFactory)
    {
    }

    /// <summary>
    /// Called when a sprite is being loaded
    /// <br/>
    /// Equivalent to a HarmonyPostFix on ResourceLoader.LoadSpriteFromSpriteReferenceAsync
    /// </summary>
    [Obsolete("No longer implemented")]
    public virtual void OnSpriteLoad(SpriteReference spriteref, Image image)
    {
    }

    /// <summary>
    /// Called when the GameData.Instance object is first loaded
    /// </summary>
    /// <param name="gameData">GameData.Instance</param>
    public virtual void OnGameDataLoaded(GameData gameData)
    {
    }

    /// <summary>
    /// Called before a sprite is loaded from a SpriteAtlas
    /// Return 'false' to prevent the original method from running
    /// <br/>
    /// Equivalent to a HarmonyPrefix on SpriteAtlas.GetSprite
    /// </summary>
    /// <param name="spriteAtlas"></param>
    /// <param name="name"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public virtual bool PreSpriteLoaded(SpriteAtlas spriteAtlas, ref string name, ref Sprite result)
    {
        return true;
    }

    /// <summary>
    /// Called after a sprite is loaded from a SpriteAtlas
    /// <br/>
    /// Equivalent to a HarmonyPostfix on SpriteAtlas.GetSprite
    /// </summary>
    /// <param name="spriteAtlas"></param>
    /// <param name="name"></param>
    /// <param name="result"></param>
    public virtual void PostSpriteLoaded(SpriteAtlas spriteAtlas, string name, ref Sprite result)
    {
    }

    #endregion

    #region Menu Hooks

    /// <summary>
    /// Called when you go to the main menu screen
    /// <br/>
    /// Equivalent to a HarmonyPostFix on MainMenu.OnEnable
    /// </summary>
    public virtual void OnMainMenu()
    {
    }

    /// <summary>
    /// Called right after a match ends in victory
    /// <br/>
    /// Equivalent to a HarmonyPostFix on InGame.OnVictory
    /// </summary>
    public virtual void OnVictory()
    {
    }

    /// <summary>
    /// Called right after a match is started up (restart included it seems like)
    /// <br/>
    /// Equivalent to a HarmonyPostFix on InGame.StartMatch
    /// </summary>
    public virtual void OnMatchStart()
    {
    }

    /// <summary>
    /// Called when a match is restarted
    /// <br/>
    /// Equivalent to a HarmonyPostFix on InGame.Restart
    /// </summary>
    public virtual void OnRestart()
    {
    }

    /// <summary>
    /// Called right after a game ends in victory
    /// <br/>
    /// Equivalent to a HarmonyPostFix on TimeManager.SetFastForward
    /// </summary>
    public virtual void OnFastForwardChanged(bool newValue)
    {
    }


    /// <summary>
    /// Called right after the game finishes loading everything
    /// <br/>
    /// Equivalent to a HarmonyPostFix on TitleScreen.Start
    /// </summary>
    public virtual void OnTitleScreen()
    {
    }


    /// <summary>
    /// Called when the player returns to the MainMenu from a match
    /// <br/>
    /// Equivalent to a HarmonyPostFix on InGame.Quit
    /// </summary>
    public virtual void OnMatchEnd()
    {
    }
    /// <summary>
    /// Called when the pause screen is opened
    /// <br/>
    /// Equivalent to a HarmonyPostfix on PauseScreen.Open
    /// </summary>
    /// <param name="pauseScreen"></param>
    public virtual void OnPauseScreenOpened(PauseScreen pauseScreen)
    {
    }


    /// <summary>
    /// Called when the pause screen is closed
    /// <br/>
    /// Equivalent to a HarmonyPostfix on PauseScreen.Close
    /// </summary>
    /// <param name="pauseScreen"></param>
    public virtual void OnPauseScreenClosed(PauseScreen pauseScreen)
    {
    }

    #endregion

    #region Bloon Hooks

    /// <summary>
    /// Called right before a Bloon would leak.
    /// Return 'false' to prevent the leak from happening
    /// <br/>
    /// Equivalent to a HarmonyPreFix on Bloon.Leaked
    /// </summary>
    public virtual bool PreBloonLeaked(Bloon bloon)
    {
        return true;
    }

    /// <summary>
    /// Called right after a Bloon leaks.
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Bloon.Leaked
    /// </summary>
    public virtual void PostBloonLeaked(Bloon bloon)
    {
    }


    /// <summary>
    /// Called right after a Bloon is first created
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Bloon.Initialise
    /// </summary>
    public virtual void OnBloonCreated(Bloon bloon)
    {
    }

    /// <summary>
    /// Called right after a Bloon's BloonModel is updated
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Bloon.UpdatedModel
    /// </summary>
    public virtual void OnBloonModelUpdated(Bloon bloon, Model model)
    {
    }


    /// <summary>
    /// Called right after a Bloon is destroyed
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Bloon.OnDestroy
    /// </summary>
    [Obsolete("No longer implemented.")]
    public virtual void OnBloonDestroy(Bloon bloon)
    {
    }

    /// <summary>
    /// Called right after a Bloon is destroyed, but only when it's popped and not leaked
    /// </summary>
    /// <param name="bloon"></param>
    [Obsolete("No longer implemented")]
    public virtual void OnBloonPopped(Bloon bloon)
    {
    }

    /// <summary>
    /// Called right after a Bloon is damaged
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Bloon.Damaged
    /// </summary>
    [Obsolete("No longer implemented")]
    public virtual void PostBloonDamaged(Bloon bloon, float totalAmount, Projectile projectile,
        bool distributeToChildren, bool overrideDistributeBlocker, bool createEffect, [Optional] Tower tower,
        [Optional] BloonProperties immuneBloonProperties, bool canDestroyProjectile = true,
        bool ignoreNonTargetable = false, bool blockSpawnChildren = false)
    {
    }

    /// <summary>
    /// Called after a new bloon emission is added to the spawner
    /// <br/>
    /// Equivalent to a HarmonyPostfix on Spawner.AddEmissions
    /// </summary>
    /// <param name="spawner"></param>
    /// <param name="newEmissions"></param>
    /// <param name="round"></param>
    /// <param name="index"></param>
    public virtual void OnBloonEmissionsAdded(Spawner spawner, Il2CppReferenceArray<BloonEmissionModel> newEmissions,
        int round, int index = 0)
    {
    }

    /// <summary>
    /// Called after a bloon is emitted from a spawner
    /// <br/>
    /// Equivalent to a HarmonyPostfix on Spawner.Emit
    /// </summary>
    /// <param name="spawner"></param>
    /// <param name="bloonModel"></param>
    /// <param name="round"></param>
    /// <param name="index"></param>
    /// <param name="startingDist"></param>
    /// <param name="bloon"></param>
    public virtual void OnBloonEmitted(Spawner spawner, BloonModel bloonModel, int round, int index, float startingDist,
        ref Bloon bloon)
    {
    }

    #endregion

    #region Tower Hooks

    /// <summary>
    /// Called right before a Tower's 3D graphics are initialized
    /// <br/>
    /// Equivalent to a HarmonyPreFix on InputManager.CreateTowerGraphicsAsync
    /// </summary>
    public virtual void OnTowerGraphicsCreated(TowerModel towerModel, List<UnityDisplayNode> placementGraphics)
    {
    }

    /// <summary>
    /// Called right after a Tower is initialized
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Tower.Initialise
    /// </summary>
    public virtual void OnTowerCreated(Tower tower, Entity target, Model modelToUse)
    {
    }

    /// <summary>
    /// Called right after a Tower is destroyed
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Tower.Destroyed
    /// </summary>
    public virtual void OnTowerDestroyed(Tower tower)
    {
    }

    /// <summary>
    /// Called right after a Tower is sold
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Tower.OnSold
    /// </summary>
    public virtual void OnTowerSold(Tower tower, float amount)
    {
    }

    /// <summary>
    /// Called right after a Tower is selected by the player
    /// <br/>
    /// Equivalent to a HarmonyPostFix on TowerSelectionMenu.Show
    /// </summary>
    public virtual void OnTowerSelected(Tower tower)
    {
    }

    /// <summary>
    /// Called right after a Tower is deselected by the player
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Tower.UnHighlight
    /// </summary>
    public virtual void OnTowerDeselected(Tower tower)
    {
    }

    /// <summary>
    /// Called right after a Tower is upgraded
    /// </summary>
    public virtual void OnTowerUpgraded(Tower tower, string upgradeName, TowerModel newBaseTowerModel)
    {
    }

    /// <summary>
    /// Called right after a Tower's TowerModel is changed for any reason (creation, upgrading, etc.)
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Tower.UpdatedModel
    /// </summary>
    public virtual void OnTowerModelChanged(Tower tower, Model newModel)
    {
    }

    /// <summary>
    /// Called at the end of each round when a Tower's data is saved
    /// <br/>
    /// Use saveData.metaData to save custom information
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Tower.GetSavedData
    /// </summary>
    public virtual void OnTowerSaved(Tower tower, TowerSaveDataModel saveData)
    {
    }

    /// <summary>
    /// Called when you load a save file and a Tower's save data get loaded for the tower
    /// <br/>
    /// Use saveData.metaData to load custom information
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Tower.SetSavedData
    /// </summary>
    public virtual void OnTowerLoaded(Tower tower, TowerSaveDataModel saveData)
    {
    }

    #endregion

    #region Simulation Hooks

    /// <summary>
    /// Called right after Cash is added in a game
    /// Tower might be null
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Simulation.AddCash
    /// </summary>
    public virtual void OnCashAdded(double amount, Simulation.CashType from,
        int cashIndex, Simulation.CashSource source, Tower tower)
    {
    }

    /// <summary>
    /// Called right after Cash is removed in a game
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Simulation.RemoveCash
    /// </summary>
    public virtual void OnCashRemoved(double amount, Simulation.CashType from, int cashIndex,
        Simulation.CashSource source)
    {
    }

    /// <summary>
    /// Called right after a round starts
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Simulation.OnRoundStart
    /// </summary>
    public virtual void OnRoundStart()
    {
    }

    /// <summary>
    /// Called right after a round starts
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Simulation.OnRoundEnd
    /// </summary>
    public virtual void OnRoundEnd()
    {
    }

    /// <summary>
    /// Called right after a match ends in defeat
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Simulation.OnDefeat
    /// </summary>
    public virtual void OnDefeat()
    {
    }

    /// <summary>
    /// Called before the game shows a hint for a specific round
    /// Return 'false' to prevent the original method from running
    /// <br/>
    /// Equivalent to a HarmonyPrefix on InGame.GetRoundHint
    /// </summary>
    /// <param name="inGame"></param>
    /// <param name="round">the current round</param>
    /// <param name="text">the text the hint will have, passed as a ref to allow changes</param>
    public virtual bool GetRoundHint(InGame inGame, int round, ref string text)
    {
        return true;
    }

    /// <summary>
    /// Called after a Removeable is destroyed
    /// <br/>
    /// Equivalent to a HarmonyPostfix on Map.DestroyRemoveable
    /// </summary>
    /// <param name="removeable"></param>
    public virtual void OnRemoveableDestroyed(Removeable removeable)
    {
    }

    #endregion

    #region Weapon/Projectile/Attack Hooks

    /// <summary>
    /// Called when a ability is cast
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Ability.Activate
    /// </summary>
    public virtual void OnAbilityCast(Ability ability)
    {
    }

    /// <summary>
    /// Called right after an Attack is created
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Attack.Initialise
    /// </summary>
    public virtual void OnAttackCreated(Attack attack, Entity entity, Model modelToUse)
    {
    }


    /// <summary>
    /// Called right after a Tower's Attack is changed for any reason (creation, upgrading, etc.)
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Attack.UpdatedModel
    /// </summary>
    public virtual void OnAttackModelChanged(Attack attack, Model newModel)
    {
    }

    /// <summary>
    /// Called right after a Weapon is created
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Weapon.Initialise
    /// </summary>
    public virtual void OnWeaponCreated(Weapon weapon, Entity entity, Model modelToUse)
    {
    }

    /// <summary>
    /// Called when a weapon fires
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Weapon.SpawnDart
    /// </summary>
    public virtual void OnWeaponFire(Weapon weapon)
    {
    }

    /// <summary>
    /// Called right after a Tower's WeaponModel is changed for any reason (creation, upgrading, etc.)
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Weapon.UpdatedModel
    /// </summary>
    public virtual void OnWeaponModelChanged(Weapon weapon, Model newModel)
    {
    }

    /// <summary>
    /// Called right after a Projectile is created
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Projectile.Initialise
    /// </summary>
    public virtual void OnProjectileCreated(Projectile projectile, Entity entity, Model modelToUse)
    {
    }


    /// <summary>
    /// Called right after a Tower's TowerModel is changed for any reason (creation, upgrading, etc.)
    /// <br/>
    /// Equivalent to a HarmonyPostFix on Projectile.UpdatedModel
    /// </summary>
    public virtual void OnProjectileModelChanged(Projectile projectile, Model newModel)
    {
    }

    /// <summary>
    /// Called when all of the existing GameObjects within a match are destroyed
    /// </summary>
    public virtual void OnGameObjectsReset()
    {
    }
    /// <summary>
    /// Called after a TowerPurchaseButton is created
    /// <br/>
    /// Equivalent to a HarmonyPostfix on ShopMenu.CreateTowerButton
    /// </summary>
    /// <param name="tower"></param>
    /// <param name="index"></param>
    /// <param name="showAmount"></param>
    /// <param name="button"></param>
    public virtual void OnTowerButtonCreated(TowerModel tower, int index, bool showAmount,
        ref TowerPurchaseButton button)
    {
    }


    /// <summary>
    /// Called before the TowerInventory is initialized
    /// <br/>
    /// Equivalent to a HarmonyPrefix on TowerInventory.Init
    /// </summary>
    /// <param name="towerInventory"></param>
    /// <param name="baseTowers"></param>
    /// <returns></returns>
    public virtual void PreTowerInventoryInit(TowerInventory towerInventory,
        ref IEnumerable<TowerDetailsModel> baseTowers)
    {
    }

    /// <summary>
    /// Called after a TowerInventory is initialized
    /// <br/>
    /// Equivalent to a HarmonyPostfix on TowerInventory.Init
    /// </summary>
    /// <param name="towerInventory"></param>
    /// <param name="baseTowers"></param>
    public virtual void OnTowerInventoryInit(TowerInventory towerInventory, List<TowerDetailsModel> baseTowers)
    {
    }

    /// <summary>
    /// Called after a banana is picked up
    /// <br/>
    /// Equivalent to a HarmonyPostfix on Cash.Pickup
    /// </summary>
    /// <param name="banana"></param>
    /// <param name="amount"></param>
    public virtual void OnBananaPickup(Cash banana, float amount)
    {
    }

    #endregion

    #region General UI Hooks

    /// <summary>
    /// Called after any button is clicked
    /// <br/>
    /// Equivalent to a HarmonyPostfix on Button.OnPointerClick
    /// </summary>
    /// <param name="button"></param>
    /// <param name="clickData"></param>
    public virtual void OnButtonClicked(Button button, PointerEventData clickData)
    {
    }
    /// <summary>
    /// Called after the mouse goes over a selectable ui element
    /// <br/>
    /// Equivalent to a HarmonyPostfix on Button.OnPointerEnter
    /// </summary>
    /// <param name="button"></param>
    /// <param name="eventData"></param>
    public virtual void OnPointerEnterSelectable(Selectable button, PointerEventData eventData)
    {
    }

    /// <summary>
    /// Called after the mouse leaves a button
    /// <br/>
    /// Equivalent to a HarmonyPostfix on Button.OnPointerExit
    /// </summary>
    /// <param name="button"></param>
    /// <param name="eventData"></param>
    public virtual void OnPointerExitSelectable(Selectable button, PointerEventData eventData)
    {
    }

    #endregion

    #region API Hooks

    /// <summary>
    /// Called whenever the Mod Options Menu gets opened, after it finishes initializing
    /// </summary>
    public virtual void OnModOptionsOpened()
    {
    }

    /// <inheritdoc cref="OnInitializeMelon"/>
    public new virtual void OnApplicationStart()
    {
    }

    /// <inheritdoc cref="OnEarlyInitializeMelon"/>
    public virtual void OnEarlyInitialize()
    {
    }

    /// <inheritdoc cref="OnInitializeMelon"/>
    public virtual void OnInitialize()
    {
    }


    /// <summary>
    /// Called when the settings for your mod are saved.
    /// </summary>
    /// <param name="settings">The json representation of the settings about to be saved</param>
    public virtual void OnSaveSettings(JObject settings)
    {
    }

    /// <summary>
    /// Called when the settings for your mod are loaded
    /// </summary>
    /// <param name="settings">The json representation of the settings that were just loaded</param>
    public virtual void OnLoadSettings(JObject settings)
    {
    }

    #endregion
    
}