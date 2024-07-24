using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace CricketVehicle
{
	[HarmonyPatch(typeof(PropulsionCannon))]
	public static class PropulsionCannonPatcher
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(PropulsionCannon.ValidateNewObject))]
		public static bool ValidateNewObjectPrefix(PropulsionCannon __instance, GameObject go)
		{
			if (go.GetComponent<CricketContainer>() != null && go.GetComponent<CricketContainer>().GetComponentInParent<Cricket>() != null)
			{
				return false;
			}
			return true;
		}
	}
}
