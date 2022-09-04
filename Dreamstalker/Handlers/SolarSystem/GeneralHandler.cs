﻿using Dreamstalker.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Dreamstalker.Handlers.SolarSystem;

[RequireComponent(typeof(Main))]
internal class GeneralHandler : SolarSystemHandler
{
    protected override void OnSolarSystemAwake() 
    {
		// Want to do this before NH starts creating any of it

		// Remove all dialogue
		foreach (var dialogue in FindObjectsOfType<CharacterDialogueTree>())
		{
			dialogue.gameObject.SetActive(false);
		}

		// Remove all Hearthians
		foreach (var controller in FindObjectsOfType<CharacterAnimController>())
		{
			controller.gameObject.SetActive(false);
		}
	}

    protected override void OnSolarSystemStart()
    {
        // Turn off all campfires (has to happen after campfire Awake
        foreach (var campfire in FindObjectsOfType<Campfire>())
        {
            campfire.SetState(Campfire.State.SMOLDERING, false);
        }
	}
}
