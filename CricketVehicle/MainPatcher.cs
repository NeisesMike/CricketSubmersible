using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Collections;
using Nautilus.Options.Attributes;
using Nautilus.Options;
using Nautilus.Json;
using Nautilus.Handlers;
using Nautilus.Utility;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Json.Attributes;

using innateStorage = System.Collections.Generic.List<System.Tuple<System.String, float>>;

namespace CricketVehicle
{
    public static class Logger
    {
        internal static ManualLogSource MyLog { get; set; }
        public static void Log(string message)
        {
            MyLog.LogInfo(message);
        }
        public static void Error(string message)
        {
            MyLog.LogError(message);
        }
        public static void Output(string msg, int x = 500, int y = 0)
        {
            BasicText message = new BasicText(x, y);
            message.ShowMessage(msg, 4);
        }
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(VehicleFramework.PluginInfo.PLUGIN_GUID)]
    [BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, Nautilus.PluginInfo.PLUGIN_VERSION)]
    public class MainPatcher : BaseUnityPlugin
    {
        internal static CricketConfig config { get; private set; }
        public static TechType cricketContainerTT { get; private set; }
        public TechType RegisterCricketContainer()
        {
            const string ccName = "CricketContainer";
            PrefabInfo ccInfo = PrefabInfo.WithTechType(ccName, ccName, "A haulable container designed for the Cricket submersible.");
            ccInfo.WithIcon(Cricket.boxCrafterSprite);
            PDAEncyclopedia.EntryData entry = new PDAEncyclopedia.EntryData
            {
                key = ccName,
                path = "Tech/Vehicles",
                nodes = new[] { "Tech", "Vehicles" },
                unlocked = true,
                popup = null,
                image = null,
            };
            LanguageHandler.SetLanguageLine("Ency_" + ccName, ccName);
            LanguageHandler.SetLanguageLine("EncyDesc_" + ccName, "The Cricket Container is a special type of floating locker that can be hauled by a Cricket Submersible.");
            Nautilus.Handlers.PDAHandler.AddEncyclopediaEntry(entry);

            CustomPrefab cricketContainerCustomPrefab = new CustomPrefab(ccInfo);
            Cricket.storageContainer.EnsureComponent<TechTag>().type = ccInfo.TechType;
            Cricket.storageContainer.EnsureComponent<PrefabIdentifier>().ClassId = ccName;
            cricketContainerCustomPrefab.SetGameObject(Cricket.storageContainer);


            RecipeData recipe = new RecipeData(new CraftData.Ingredient(TechType.Titanium, 2), new CraftData.Ingredient(TechType.Copper, 1));

            cricketContainerCustomPrefab
                .SetRecipe(recipe)
                .WithCraftingTime(5)
                .WithFabricatorType(CraftTree.Type.Constructor)
                .WithStepsToFabricatorTab(new string[] { "Vehicles" });
            cricketContainerCustomPrefab.SetPdaGroupCategory(TechGroup.Constructor, TechCategory.Constructor);
            cricketContainerCustomPrefab.SetUnlock(TechType.Constructor);

            cricketContainerCustomPrefab.Register();

            PingType myPT = VehicleFramework.VehicleManager.RegisterPingType((PingType)225);

            // Add this ping sprite to the VF master list, so it's patched in correctly
            var ccPingInstance = Cricket.storageContainer.EnsureComponent<PingInstance>();
            ccPingInstance.origin = Cricket.storageContainer.transform;
            ccPingInstance.pingType = myPT;
            ccPingInstance.SetLabel("Vehicle");
            VehicleFramework.VehicleManager.mvPings.Add(ccPingInstance);
            VehicleFramework.Assets.SpriteHelper.RegisterPingSprite(Cricket.storageContainer.name, myPT, Cricket.cratePingSprite);

            return ccInfo.TechType;
        }
        public void Awake()
        {
            CricketVehicle.Logger.MyLog = base.Logger;
            Cricket.GetAssets();
            cricketContainerTT = RegisterCricketContainer();
        }
        public void Start()
        {
            config = OptionsPanelHandler.RegisterModOptions<CricketConfig>();
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            UWE.CoroutineHost.StartCoroutine(Cricket.Register());
        }
    }

    [Menu("Cricket Vehicle Options")]
    public class CricketConfig : ConfigFile
    {
        [Keybind("Attach/Detach Cricket Container", Tooltip = "Manage the container mount on the Cricket submersible")]
        public KeyCode attach = KeyCode.F;
    }
}
