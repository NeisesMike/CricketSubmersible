﻿using System;
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
        internal static SaveData ContainerSaveData { get; private set; }

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
            VehicleFramework.VehicleManager.vehicleTypes.Add(new VehicleFramework.VehicleEntry(Cricket.storageContainer, 225, myPT, Cricket.cratePingSprite, ccInfo.TechType));

            Constructor.SpawnPoint sp = new Constructor.SpawnPoint();

            return ccInfo.TechType;
        }
        public void Awake()
        {
            CricketVehicle.Logger.MyLog = base.Logger;
            Cricket.GetAssets();
            cricketContainerTT = RegisterCricketContainer();
            SaveData saveData = SaveDataHandler.RegisterSaveDataCache<SaveData>();

            // Update the player position before saving it
            saveData.OnStartedSaving += (object sender, JsonFileEventArgs e) =>
            {
                SaveData data = e.Instance as SaveData;
                data.InnateStorages = SerializeStorage();
                data.AttachmentStatuses = SerializeAttachmentStatuses();
            };

            saveData.OnFinishedLoading += (object sender, JsonFileEventArgs e) =>
            {
                ContainerSaveData = e.Instance as SaveData;
                UWE.CoroutineHost.StartCoroutine(SaveUtils.ReattachContainers(ContainerSaveData));
            };
        }


        public void Start()
        {
            config = OptionsPanelHandler.RegisterModOptions<CricketConfig>();
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            UWE.CoroutineHost.StartCoroutine(Cricket.Register());
        }


        internal static List<Tuple<Vector3, innateStorage>> SerializeStorage()
        {
            List<Tuple<Vector3, innateStorage>> allVehiclesStoragesContents = new List<Tuple<Vector3, innateStorage>>();
            foreach (CricketContainer cc in VehicleFramework.Admin.GameObjectManager<CricketContainer>.AllSuchObjects)
            {
                if (cc == null)
                {
                    continue;
                }
                if (!cc.name.Contains("Clone"))
                {
                    // skip the prefabs
                    continue;
                }
                innateStorage thisContents = new innateStorage();
                foreach (var item in cc.storageContainer.container.ToList())
                {
                    TechType thisItemType = item.item.GetTechType();
                    float batteryChargeIfApplicable = -1;
                    var bat = item.item.GetComponentInChildren<Battery>(true);
                    if (bat != null)
                    {
                        batteryChargeIfApplicable = bat.charge;
                    }
                    thisContents.Add(new Tuple<System.String, float>(thisItemType.AsString(), batteryChargeIfApplicable));
                }
                allVehiclesStoragesContents.Add(new Tuple<Vector3, innateStorage>(cc.transform.position, thisContents));
            }
            return allVehiclesStoragesContents;
        }
        internal static IEnumerator DeserializeStorage(CricketContainer cc)
        {
            while (!LargeWorldStreamer.main || !LargeWorldStreamer.main.IsReady() || !LargeWorldStreamer.main.IsWorldSettled())
            {
                yield return null;
            }
            while (WaitScreen.IsWaiting)
            {
                yield return null;
            }
            yield return new WaitForSeconds(3.2f);
            List<Tuple<Vector3, innateStorage>> allCricketContainers = ContainerSaveData.InnateStorages;
            // try to match against a saved vehicle in our list
            foreach (var container in allCricketContainers)
            {
                if (Vector3.Distance(cc.transform.position, container.Item1) < 1)
                {
                    foreach (var item in container.Item2)
                    {
                        TaskResult<GameObject> result = new TaskResult<GameObject>();
                        bool resulty = TechTypeExtensions.FromString(item.Item1, out TechType thisTT, true);
                        yield return CraftData.InstantiateFromPrefabAsync(thisTT, result, false);
                        GameObject thisItem = result.Get();
                        if (item.Item2 >= 0)
                        {
                            // check whether we *are* a battery xor we *have* a battery
                            if (thisItem.GetComponent<Battery>() != null)
                            {
                                // we are a battery
                                var bat = thisItem.GetComponentInChildren<Battery>();
                                bat.charge = item.Item2;
                            }
                            else
                            {
                                // we have a battery (we are a tool)
                                // Thankfully we have this naming convention
                                Transform batSlot = thisItem.transform.Find("BatterySlot");
                                result = new TaskResult<GameObject>();
                                yield return CraftData.InstantiateFromPrefabAsync(TechType.Battery, result, false);
                                GameObject newBat = result.Get();
                                newBat.GetComponent<Battery>().charge = item.Item2;
                                newBat.transform.SetParent(batSlot);
                                newBat.SetActive(false);
                            }
                        }
                        thisItem.transform.SetParent(cc.storageContainer.storageRoot.transform);
                        cc.storageContainer.container.AddItem(thisItem.GetComponent<Pickupable>());
                        thisItem.SetActive(false);
                    }
                }
            }
            yield break;
        }
        internal static List<Tuple<Vector3, bool>> SerializeAttachmentStatuses()
        {
            return VehicleFramework.Admin.GameObjectManager<CricketContainer>.AllSuchObjects
                .Where(x => x != null && x.transform != null)
                .Select(x => new Tuple<Vector3, bool>(x.transform.position, SaveUtils.IsAttached(x)))
                .ToList();
        }
    }
    [FileName("cricket_containers")]
    internal class SaveData : SaveDataCache
    {
        // location of the storage container : storage contents
        public List<Tuple<Vector3, innateStorage>> InnateStorages { get; set; }
        // location of the storage container : attachment status (true only if this container was attached at save-time)
        public List<Tuple<Vector3, bool>> AttachmentStatuses { get; set; }
    }

    [Menu("Cricket Vehicle Options")]
    public class CricketConfig : ConfigFile
    {
        [Keybind("Attach/Detach Cricket Container", Tooltip = "Manage the container mount on the Cricket submersible")]
        public KeyCode attach = KeyCode.F;
    }
}
