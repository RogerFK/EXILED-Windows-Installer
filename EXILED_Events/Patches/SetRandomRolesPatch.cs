﻿using System;
using Harmony;
using Mirror;
using MEC;

namespace EXILED.Patches
{
    [HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.SetRandomRoles))]
    public class SetRandomRolesPatch
    {
        public static bool Prefix(CharacterClassManager __instance)
        {
            if (EventPlugin.SetRandomRolesPatchDisable)
                return true;

            try
            {
                if (__instance.isLocalPlayer && __instance.isServer)
                {
                    __instance.RunSmartClassPicker();
                }
                if (NetworkServer.active)
                {
                    Timing.RunCoroutine(__instance.MakeSureToSetHP(), Segment.FixedUpdate);
                }
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"SetRandomRoles error: {e}");
                return true;
            }
        }
    }
}