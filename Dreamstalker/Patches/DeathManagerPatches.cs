﻿using Dreamstalker.Components;
using Dreamstalker.Handlers.SolarSystem;
using Dreamstalker.Utility;
using HarmonyLib;

namespace Dreamstalker.Patches;

[HarmonyPatch]
internal static class DeathManagerPatches
{
	public static bool flagEnd;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
	private static bool DeathManager_KillPlayer(DeathManager __instance, DeathType deathType)
	{
		if (flagEnd)
		{
			LoadManager.LoadScene(OWScene.TitleScreen, LoadManager.FadeType.ToBlack, 0.01f);
			flagEnd = false;
		}

		// The time loop should be disabled but just in case
		if (deathType != DeathType.TimeLoop)
		{
			PlayerEffectController.Instance.Blink(1f);
			PlayerSpawnUtil.Respawn();
			PropHandler.TurnOffCampFires();
			return false;
		}

		return true;
	}
}
