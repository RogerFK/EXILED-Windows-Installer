﻿using System;
using Harmony;
using Mirror;
using UnityEngine;

namespace EXILED.Patches
{
	[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.AllowContain))]
	public class FemurEnterEvent
	{
		public static bool Prefix(CharacterClassManager __instance)
		{
			if (EventPlugin.FemurEnterEventDisable)
				return true;

			try
			{
				if (!NetworkServer.active || !NonFacilityCompatibility.currentSceneSettings.enableStandardGamplayItems)
					return false;
				foreach (GameObject player in PlayerManager.players)
				{
					if ((double)Vector3.Distance(player.transform.position, __instance._lureSpj.transform.position) < 1.97000002861023)
					{
						CharacterClassManager component1 = player.GetComponent<CharacterClassManager>();
						PlayerStats component2 = player.GetComponent<PlayerStats>();
						if (component1.Classes.SafeGet(component1.CurClass).team != Team.SCP && component1.CurClass != RoleType.Spectator && !component1.GodMode)
						{
							bool allow = true;
							Events.InvokeFemurEnterEvent(component2.gameObject, ref allow);
							if (allow)
							{
								component2.HurtPlayer(new PlayerStats.HitInfo(10000f, "WORLD", DamageTypes.Lure, 0), player);
								__instance._lureSpj.SetState(true);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Error($"FemurEnterEvent event error: {e}");
				return true;
			}
			return false;
		}
	}
}
