using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Reflection;

using UnityEngine.U2D;
using VehicleFramework.VehicleBuilding;
using VehicleFramework.VehicleTypes;

namespace CricketVehicle
{
    public partial class Cricket : Submersible
    {
        public static GameObject model = null;
        public static GameObject controlPanel = null;
        public static UnityEngine.Sprite pingSprite = null;
        public static Sprite saveSprite = null;
        public static UnityEngine.Sprite cratePingSprite = null;
        public static UnityEngine.Sprite crafterSprite = null;
        public static UnityEngine.Sprite boxCrafterSprite = null;
        public static GameObject storageContainer = null;
        
        public static void GetAssets()
        {
            // load the asset bundle
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var myLoadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(modPath, "assets/cricket"));
            if (myLoadedAssetBundle == null)
            {
                Logger.Log("Failed to load AssetBundle!");
                return;
            }

            System.Object[] arr = myLoadedAssetBundle.LoadAllAssets();
            foreach (System.Object obj in arr)
            {
                if (obj.ToString().Contains("SpriteAtlas"))
                {
                    SpriteAtlas thisAtlas = (SpriteAtlas)obj;
                    pingSprite = saveSprite = thisAtlas.GetSprite("PingSprite");
                    cratePingSprite = thisAtlas.GetSprite("BoxSprite");
                    crafterSprite = thisAtlas.GetSprite("CrafterSprite");
                    boxCrafterSprite = thisAtlas.GetSprite("BoxCrafterSprite");
                }
                else if (obj.ToString().Contains("Cricket"))
                {
                    model = (GameObject)obj;
                }
                else if (obj.ToString().Contains("SFCrate"))
                {
                    storageContainer = (GameObject)obj;
                    var cc = Cricket.storageContainer.EnsureComponent<CricketContainer>();
                    cc.SetupGameObjectPregame();
                }
                else
                {
                    Logger.Log(obj.ToString());
                }
            }
        }

        public override Dictionary<TechType, int> Recipe
        {
            get
            {
                Dictionary<TechType, int> recipe = new Dictionary<TechType, int>();
                recipe.Add(TechType.TitaniumIngot, 1);
                recipe.Add(TechType.PowerCell, 1);
                recipe.Add(TechType.EnameledGlass, 2);
                recipe.Add(TechType.Lubricant, 1);
                recipe.Add(TechType.Lead, 1);
                recipe.Add(TechType.WiringKit, 1);
                return recipe;
            }
        }

        public static IEnumerator Register()
        {
            Submersible cricket = model.EnsureComponent<Cricket>() as Submersible;
            yield return UWE.CoroutineHost.StartCoroutine(VehicleFramework.Admin.VehicleRegistrar.RegisterVehicle(cricket));
        }

        public override string vehicleDefaultName
        {
            get
            {
                Language main = Language.main;
                if (!(main != null))
                {
                    return "CRICKET";
                }
                return main.Get("Cricket");
            }
        }

        public override string Description => "A small spherical submersible built for rapid forward movement of personal and cargo.";

        public override string EncyclopediaEntry
        {
            get
            {
                /*
                 * The Formula:
                 * 2 or 3 sentence blurb
                 * Features
                 * Advice
                 * Ratings
                 * Kek
                 */
                string ency = "The Cricket is a submersible designed for rapid movement of personal and cargo through tight spaces.";
                ency += "Its speed and size are what earned it the name. \n";
                ency += "\nIt features:\n";
                ency += "- A mount for one Cricket Container (built separately). \n";
                ency += "- Rapid acceleration in all directions, but only a high top speed moving forward. \n";
                ency += "- One power cell in each of the two small thrusters. \n";
                ency += "\nRatings:\n";
                ency += "- Top Speed: 12m/s \n";
                ency += "- Acceleration: 6m/s/s \n";
                ency += "- Distance per Power Cell: 7km \n";
                ency += "- Crush Depth: 250 \n";
                ency += "- Max Crush Depth (upgrade required): 1100 \n";
                ency += "- Upgrade Slots: 4 \n";
                ency += "- Dimensions: 3.5m x 3.5m x 3.1m \n";
                ency += "- Persons: 1\n";
                ency += "\n\"If you get scared, jump on outta there.\" ";
                return ency;
            }
        }

        public override GameObject VehicleModel => model;

        public override GameObject StorageRootObject
        {
            get
            {
                return transform.Find("StorageRoot").gameObject;
            }
        }

        public override GameObject ModulesRootObject
        {
            get
            {
                return transform.Find("ModulesRoot").gameObject;
            }
        }

        public override VehiclePilotSeat PilotSeat
        {
            get
            {
                VehicleFramework.VehicleBuilding.VehiclePilotSeat vps = new VehicleFramework.VehicleBuilding.VehiclePilotSeat();
                Transform mainSeat = transform.Find("Chair");
                vps.Seat = mainSeat.gameObject;
                vps.SitLocation = mainSeat.Find("SitPosition").gameObject;
                vps.LeftHandLocation = mainSeat;
                vps.RightHandLocation = mainSeat;
                return vps;
            }
        }

        public override List<VehicleHatchStruct> Hatches
        {
            get
            {
                var list = new List<VehicleFramework.VehicleBuilding.VehicleHatchStruct>();

                VehicleFramework.VehicleBuilding.VehicleHatchStruct interior_vhs = new VehicleFramework.VehicleBuilding.VehicleHatchStruct();
                Transform intHatch = transform.Find("Hatch");
                interior_vhs.Hatch = intHatch.gameObject;
                interior_vhs.ExitLocation = intHatch.Find("ExitPosition");
                interior_vhs.SurfaceExitLocation = intHatch.Find("ExitPosition");
                list.Add(interior_vhs);


                VehicleFramework.VehicleBuilding.VehicleHatchStruct interior_vhs2 = new VehicleFramework.VehicleBuilding.VehicleHatchStruct();
                interior_vhs2.Hatch = transform.Find("CollisionModel/Sphere").gameObject;
                interior_vhs2.ExitLocation = interior_vhs.ExitLocation;
                interior_vhs2.SurfaceExitLocation = interior_vhs.ExitLocation;
                list.Add(interior_vhs2);
                
                return list;
            }
        }

        public override List<VehicleStorage> InnateStorages
        {
            get
            {
                var list = new List<VehicleFramework.VehicleBuilding.VehicleStorage>();
                return list;
            }
        }

        public override List<VehicleStorage> ModularStorages
        {
            get
            {
                var list = new List<VehicleFramework.VehicleBuilding.VehicleStorage>();

                VehicleFramework.VehicleBuilding.VehicleStorage thisVS = new VehicleFramework.VehicleBuilding.VehicleStorage();
                Transform thisStorage = transform.Find("SFCrate");
                thisVS.Container = thisStorage.gameObject;
                thisVS.Height = 6;
                thisVS.Width = 5;
                list.Add(thisVS);

                return list;
            }
        }

        public override List<VehicleUpgrades> Upgrades
        {
            get
            {
                var list = new List<VehicleFramework.VehicleBuilding.VehicleUpgrades>();
                VehicleFramework.VehicleBuilding.VehicleUpgrades vu = new VehicleFramework.VehicleBuilding.VehicleUpgrades();
                vu.Interface = transform.Find("CollisionModel/MainProp").gameObject;
                vu.Flap = vu.Interface;
                list.Add(vu);
                return list;
            }
        }

        public override List<VehicleBattery> Batteries
        {
            get
            {
                var list = new List<VehicleFramework.VehicleBuilding.VehicleBattery>();

                VehicleFramework.VehicleBuilding.VehicleBattery vb1 = new VehicleFramework.VehicleBuilding.VehicleBattery();
                vb1.BatterySlot = transform.Find("CollisionModel/LeftProp").gameObject;
                list.Add(vb1);

                VehicleFramework.VehicleBuilding.VehicleBattery vb2 = new VehicleFramework.VehicleBuilding.VehicleBattery();
                vb2.BatterySlot = transform.Find("CollisionModel/RightProp").gameObject;
                list.Add(vb2);

                return list;
            }
        }

        public override List<VehicleFloodLight> HeadLights
        {
            get
            {
                var list = new List<VehicleFramework.VehicleBuilding.VehicleFloodLight>();

                VehicleFramework.VehicleBuilding.VehicleFloodLight mainLight = new VehicleFramework.VehicleBuilding.VehicleFloodLight
                {
                    Light = transform.Find("lights_parent/HeadLights/Main").gameObject,
                    Angle = 70,
                    Color = Color.white,
                    Intensity = 1.3f,
                    Range = 90f
                };
                list.Add(mainLight);

                return list;
            }
        }

        public override List<GameObject> WaterClipProxies => new List<GameObject> { transform.Find("CollisionModel/Sphere").gameObject };

        public override List<GameObject> CanopyWindows => new List<GameObject> { transform.Find("Canopy").gameObject };

        public override BoxCollider BoundingBoxCollider => transform.Find("BoundingBox")?.gameObject.GetComponent<BoxCollider>();

        public override GameObject[] CollisionModel => new GameObject[] { transform.Find("CollisionModel").gameObject };
        
        public override VehicleFramework.Engines.VFEngine VFEngine
        {
            get
            {
                return gameObject.EnsureComponent<VehicleFramework.Engines.CricketEngine>();
            }
        }

        public override UnityEngine.Sprite PingSprite => pingSprite;

        public override Sprite SaveFileSprite => saveSprite;

        // Degasi 1 @ 250
        // seamoth at 200 now
        public override int BaseCrushDepth => 250;
        // 400
        // seamoth at 300 now
        public override int CrushDepthUpgrade1 => 150;
        // 650
        // Degasi 2 @ 500
        // seamoth at 500 now
        public override int CrushDepthUpgrade2 => 250;
        // 1100, Lost River
        // 1700 end game
        // seamoth at 900 now
        public override int CrushDepthUpgrade3 => 450;

        public override int MaxHealth => 420;

        public override int Mass => 500;

        public override int NumModules => 4;

        public override bool HasArms => false;
        public override UnityEngine.Sprite CraftingSprite
        {
            get
            {
                return crafterSprite;
            }
        }
        public static readonly Color earthbornCricket = new Color(165f / 255f, 192f / 255f, 155f / 255f, 1f);
        public override Color ConstructionGhostColor { get; set; } = earthbornCricket;
        public override Color ConstructionWireframeColor { get; set; } = earthbornCricket;
        public override float TimeToConstruct { get; set; } = 12f;
    }
}
