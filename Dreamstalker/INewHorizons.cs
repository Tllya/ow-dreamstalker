﻿using OWML.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Dreamstalker;

public interface INewHorizons
{
	/// <summary>
	/// Will load all configs in the regular folders (planets, systems, translations, etc) for this mod.
	/// The NH addon config template is just a single call to this API method.
	/// </summary>
	void LoadConfigs(IModBehaviour mod);

	/// <summary>
	/// Retrieve the root GameObject of a custom planet made by creating configs. 
	/// Will only work if the planet has been created (see GetStarSystemLoadedEvent)
	/// </summary>
	GameObject GetPlanet(string name);

	/// <summary>
	/// The name of the current star system loaded.
	/// </summary>
	string GetCurrentStarSystem();

	/// <summary>
	/// An event invoked when the player begins loading the new star system, before the scene starts to load.
	/// Gives the name of the star system being switched to.
	/// </summary>
	UnityEvent<string> GetChangeStarSystemEvent();

	/// <summary>
	/// An event invoked when NH has finished generating all planets for a new star system.
	/// Gives the name of the star system that was just loaded.
	/// </summary>
	UnityEvent<string> GetStarSystemLoadedEvent();

	/// <summary>
	/// Allows you to overwrite the default system. This is where the player is respawned after dying.
	/// </summary>
	bool SetDefaultSystem(string name);

	/// <summary>
	/// Allows you to instantly begin a warp to a new star system.
	/// Will return false if that system does not exist (cannot be warped to).
	/// </summary>
	bool ChangeCurrentStarSystem(string name);

	/// <summary>
	/// Returns the uniqueIDs of each installed NH addon.
	/// </summary>
	string[] GetInstalledAddons();

	/// <summary>
	/// Allows you to spawn a copy of a prop by specifying its path.
	/// This is the same as using Props->details in a config, but also returns the spawned gameObject to you.
	/// </summary>
	GameObject SpawnObject(GameObject planet, Sector sector, string propToCopyPath, Vector3 position, Vector3 eulerAngles,
		float scale, bool alignWithNormal);

	/// <summary>
	/// Allows you to spawn an AudioSignal on a planet.
	/// This is the same as using Props->signals in a config, but also returns the spawned AudioSignal to you.
	/// This method will not set its position. You will have to do that with the returned object.
	/// </summary>
	AudioSignal SpawnSignal(IModBehaviour mod, GameObject root, string audio, string name, string frequency,
		float sourceRadius = 1f, float detectionRadius = 20f, float identificationRadius = 10f, bool insideCloak = false,
		bool onlyAudibleToScope = true, string reveals = "");

	/// <summary>
	/// Allows you to spawn character dialogue on a planet. Also returns the RemoteDialogueTrigger if remoteTriggerRadius is specified.
	/// This is the same as using Props->dialogue in a config, but also returns the spawned game objects to you.
	/// This method will not set the position of the dialogue or remote trigger. You will have to do that with the returned objects.
	/// </summary>
	(CharacterDialogueTree, RemoteDialogueTrigger) SpawnDialogue(IModBehaviour mod, GameObject root, string xmlFile, float radius = 1f,
		float range = 1f, string blockAfterPersistentCondition = null, float lookAtRadius = 1f, string pathToAnimController = null,
		float remoteTriggerRadius = 0f);
}
