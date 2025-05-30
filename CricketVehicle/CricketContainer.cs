using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VehicleFramework;
using VehicleFramework.Localization;

namespace CricketVehicle
{
	public class CricketStorageInput : HandTarget, IHandTarget
	{
		public int slotID = -1;
		public GameObject model;
		public Collider collider;
		public float timeOpen = 0.5f;
		public float timeClose = 0.25f;
		public FMODAsset openSound;
		public FMODAsset closeSound;
		protected Transform tr;
		protected Vehicle.DockType dockType;
		protected bool state;
		ItemsContainer myContainer;

		public void OpenFromExternal()
		{
			PDA pda = Player.main.GetPDA();
			Inventory.main.SetUsedStorage(myContainer, false);
			pda.Open(PDATab.Inventory, null, null);
		}

		protected void OpenPDA()
		{
			PDA pda = Player.main.GetPDA();
			Inventory.main.SetUsedStorage(myContainer, false);
			if (!pda.Open(PDATab.Inventory, this.tr, new PDA.OnClose(this.OnClosePDA)))
			{
				this.OnClosePDA(pda);
				return;
			}
		}

		public override void Awake()
		{
			base.Awake();
			this.tr = GetComponent<Transform>();
			this.UpdateColliderState();

			// go up in the transform heirarchy until we find
			SetEnabled(true);
		}
		public void Start()
		{
			myContainer = GetComponent<InnateStorageContainer>().container;
		}
		protected void OnDisable()
		{

		}
		protected void ChangeFlapState()
		{
			//Utils.PlayFMODAsset(open ? this.openSound : this.closeSound, base.transform, 1f);
			Utils.PlayFMODAsset(this.openSound, base.transform, 1f);
			OpenPDA();
		}
		protected void OnClosePDA(PDA pda)
		{
			seq.Set(0, false, null);
			Utils.PlayFMODAsset(this.closeSound, base.transform, 1f);
		}
		protected void UpdateColliderState()
		{
			if (this.collider != null)
			{
				this.collider.enabled = (this.state && this.dockType != Vehicle.DockType.Cyclops);
			}
		}
		public void SetEnabled(bool state)
		{
			if (this.state == state)
			{
				return;
			}
			this.state = state;
			this.UpdateColliderState();
			if (this.model != null)
			{
				this.model.SetActive(state);
			}
		}
		public void SetDocked(Vehicle.DockType dockType)
		{
			this.dockType = dockType;
			this.UpdateColliderState();
		}
		public void OnHandHover(GUIHand hand)
		{
			HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Localizer<EnglishString>.GetString(EnglishString.OpenStorage));
			HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
		}
		public Sequence seq = new Sequence();
		public void Update()
		{
			seq.Update();
		}
		public void OnHandClick(GUIHand hand)
		{
			seq.Set(0, true, new SequenceCallback(ChangeFlapState));
		}
    }

	public class CricketContainer : MonoBehaviour, IProtoTreeEventListener
	{
		public InnateStorageContainer storageContainer;
		public float marginOfError = 0.9f;
		public static void ApplyShaders(GameObject mv)
		{
			// Add the marmoset shader to all renderers
			Shader marmosetShader = Shader.Find("MarmosetUBER");
			foreach (var renderer in mv.gameObject.GetComponentsInChildren<MeshRenderer>(true))
			{
				foreach (Material mat in renderer.materials)
				{
					mat.shader = marmosetShader;
				}
			}
			var ska = mv.gameObject.EnsureComponent<SkyApplier>();
			ska.anchorSky = Skies.Auto;
			ska.customSkyPrefab = null;
			ska.dynamic = true;
			ska.emissiveFromPower = false;
			//ska.environmentSky = null;
			var rends = mv.gameObject.GetComponentsInChildren<Renderer>();
			ska.renderers = new Renderer[rends.Count()];
			foreach (var rend in rends)
			{
				ska.renderers.Append(rend);
			}
		}
		public void SetupGameObjectPregame()
		{
			gameObject.SetActive(false);
			ApplyShaders(Cricket.storageContainer);

			var rb = gameObject.EnsureComponent<Rigidbody>();
			rb.isKinematic = false;
			rb.useGravity = false;
			rb.mass = 120;
			rb.drag = 10f;
			rb.angularDrag = 1f;

			gameObject.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Global;

			gameObject.SetActive(true);
		}
		public void SetupGameObjectWakeTime()
		{
			storageContainer = gameObject.EnsureComponent<InnateStorageContainer>();
			storageContainer.storageRoot = transform.Find("StorageRoot").gameObject.AddComponent<ChildObjectIdentifier>();
			storageContainer.storageLabel = "Cricket Container";
			storageContainer.height = 6;
			storageContainer.width = 5;

			FMODAsset storageCloseSound = SeamothHelper.Seamoth.transform.Find("Storage/Storage1").GetComponent<SeamothStorageInput>().closeSound;
			FMODAsset storageOpenSound = SeamothHelper.Seamoth.transform.Find("Storage/Storage1").GetComponent<SeamothStorageInput>().openSound;
			var inp = gameObject.EnsureComponent<CricketStorageInput>();

			inp.model = gameObject;
			inp.openSound = storageOpenSound;
			inp.closeSound = storageCloseSound;

			VehicleBuilder.CopyComponent<WorldForces>(SeamothHelper.Seamoth.GetComponent<SeaMoth>().worldForces, gameObject);
			var wf = gameObject.GetComponent<WorldForces>();
			wf.useRigidbody = GetComponent<Rigidbody>();
			wf.underwaterGravity = 0f;
			wf.aboveWaterGravity = 9.8f;
			wf.waterDepth = Ocean.GetOceanLevel();
			wf.handleGravity = false;
		}
		public void Awake()
		{
			gameObject.SetActive(false);
			SetupGameObjectWakeTime();
			gameObject.SetActive(true);
		}
		public void Start()
		{
			UWE.CoroutineHost.StartCoroutine(LoadContainer());
			UWE.CoroutineHost.StartCoroutine(RegisterWithManager());
		}
		public IEnumerator RegisterWithManager()
		{
			while (!VehicleFramework.Admin.GameStateWatcher.IsPlayerStarted)
			{
				yield return null;
			}
			VehicleFramework.Admin.GameObjectManager<CricketContainer>.Register(this);
		}
		public void OnDestroy()
		{
			//CricketContainerManager.main.DeregisterCricketContainer(this);
		}

		public void CricketContainerConstructionBeginning()
		{
			GetComponent<PingInstance>().enabled = false;
		}
		public void SubConstructionComplete()
		{
			GetComponent<WorldForces>().handleGravity = true;
			GetComponent<PingInstance>().enabled = true;
		}
		public void FixedUpdate()
		{
			if (GetComponentInParent<Cricket>() == null)
			{
				float zCorrection = Mathf.Abs(transform.eulerAngles.z - 180f);
				if (zCorrection <= 178f)
				{
					float d = Mathf.Clamp01(1f - zCorrection / 180f) * 20f;
					GetComponent<Rigidbody>().AddTorque(transform.forward * d * Time.fixedDeltaTime * Mathf.Sign(transform.eulerAngles.z - 180f), ForceMode.VelocityChange);
				}

				float xCorrection = Mathf.Abs(transform.eulerAngles.x - 180f);
				if (xCorrection <= 178f)
				{
					float d = Mathf.Clamp01(1f - xCorrection / 180f) * 20f;
					GetComponent<Rigidbody>().AddTorque(transform.right * d * Time.fixedDeltaTime * Mathf.Sign(transform.eulerAngles.x - 180f), ForceMode.VelocityChange);
				}
			}
			if (transform.position.y > 0)
			{
				GetComponent<WorldForces>().handleGravity = true;
			}
		}

		internal Cricket GetParentCricket()
		{
			return VehicleFramework.VehicleManager.VehiclesInPlay
				.Where(x => (x as Cricket) != null)
				.Select(x => x as Cricket)
				.Where(x => x.currentMountedContainer == this)
				.FirstOrDefault();
		}
		internal Cricket GetCricketWithID(string id)
        {
			return VehicleFramework.VehicleManager.VehiclesInPlay
				.Where(x => (x as Cricket) != null)
				.Select(x => x as Cricket)
				.Where(x => x.GetComponent<PrefabIdentifier>().Id.Equals(id, StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault();
		}

		private string SaveFileName => $"CricketContainer-{GetComponent<PrefabIdentifier>().Id}";
		private const string NoCricketName = "NoCricket";
		private List<Tuple<TechType, float, TechType>> GetStorageContents()
		{
			List<Tuple<TechType, float, TechType>> result = new List<Tuple<TechType, float, TechType>>();
			foreach (var item in storageContainer.container.ToList())
			{
				TechType thisItemType = item.item.GetTechType();
				float batteryChargeIfApplicable = -1;
				var bat = item.item.GetComponentInChildren<Battery>(true);
				TechType innerBatteryTT = TechType.None;
				if (bat != null)
				{
					batteryChargeIfApplicable = bat.charge;
					innerBatteryTT = bat.gameObject.GetComponent<TechTag>().type;
				}
				result.Add(new Tuple<TechType, float, TechType>(thisItemType, batteryChargeIfApplicable, innerBatteryTT));
			}
			return result;
		}
		private IEnumerator LoadStorageContents(List<Tuple<TechType, float, TechType>> contents)
		{
			TaskResult<GameObject> result = new TaskResult<GameObject>();
			foreach (var item in contents)
			{
				yield return CraftData.InstantiateFromPrefabAsync(item.Item1, result, false);
				GameObject thisItem = result.Get();

				thisItem.transform.SetParent(storageContainer.storageRoot.transform);
				try
				{
					storageContainer.container.AddItem(thisItem.GetComponent<Pickupable>());
				}
				catch (Exception e)
				{
					Logger.Error($"Failed to add storage item {thisItem.name} to cricket container {gameObject.name}");
					Logger.Log(e.Message);
					Logger.Log(e.StackTrace);
				}
				thisItem.SetActive(false);
				if (item.Item2 >= 0)
				{
					// then we have a battery xor we are a battery
					try
					{
						UWE.CoroutineHost.StartCoroutine(VehicleFramework.SaveLoad.SaveLoadUtils.ReloadBatteryPower(thisItem, item.Item2, item.Item3));
					}
					catch (Exception e)
					{
						Logger.Error($"Failed to reload battery power for cricket container item {thisItem.name} in cricket container {gameObject.name}");
						Logger.Log(e.Message);
						Logger.Log(e.StackTrace);
					}
				}
			}
		}
		void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
		{
			Cricket attached = GetParentCricket();
			Tuple<string, List<Tuple<TechType, float, TechType>>> saveData = new Tuple<string, List<Tuple<TechType, float, TechType>>>
				(
					attached == null ? NoCricketName : attached.GetComponent<PrefabIdentifier>().Id,
					GetStorageContents()
				);
			VehicleFramework.SaveLoad.JsonInterface.Write(SaveFileName, saveData);
		}
		void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
		{
			// do nothing
			// for some reason, this doesn't get called!
			// so do the same job in MonoBehaviour.Start
		}
		private IEnumerator LoadContainer()
		{
			yield return new WaitUntil(() => true);
			var savedata = VehicleFramework.SaveLoad.JsonInterface.Read<Tuple<string, List<Tuple<TechType, float, TechType>>>>(SaveFileName);
			if (savedata == default)
			{
				yield break;
			}
			if (savedata.Item2 != null)
			{
				UWE.CoroutineHost.StartCoroutine(LoadStorageContents(savedata.Item2));
			}
			if (!savedata.Item1.Equals(NoCricketName, StringComparison.OrdinalIgnoreCase))
			{
				yield return new WaitUntil(() => GetCricketWithID(savedata.Item1) != null);
				GetCricketWithID(savedata.Item1).AttachContainer(this);
			}
		}
	}
}
