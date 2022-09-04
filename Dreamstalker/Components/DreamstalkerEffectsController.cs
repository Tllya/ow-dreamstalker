﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;

namespace Dreamstalker.Components;

internal class DreamstalkerEffectsController : MonoBehaviour
{
	private Animator _animator;
	private DreamstalkerController _controller;
	private OWAudioSource _oneShotAudioSource;

	private Vector2 _smoothedMoveSpeed = Vector2.zero;
	private float _smoothedTurnSpeed;
	private DampedSpring2D _moveSpeedSpring = new DampedSpring2D(50f, 1f);
	private DampedSpring _turnSpeedSpring = new DampedSpring(50f, 1f);

	private bool _moving;

	public UnityEvent SnapNeck = new();
	public UnityEvent LiftPlayer = new();

	public void Awake()
	{
		_animator = GetComponent<Animator>();
		_controller = GetComponent<DreamstalkerController>();

		gameObject.AddComponent<AudioSource>().spatialBlend = 1f;
		_oneShotAudioSource = gameObject.AddComponent<OWAudioSource>();
		_oneShotAudioSource.SetTrack(OWAudioMixer.TrackName.Environment);

		ToggleWalk(false, true);
	}

	public enum AnimationKeys
	{
		Default,
		Grab,
		SnapNeck,
		CallForHelp
	}

	public void PlayAnimation(AnimationKeys key)
	{
		switch (key)
		{
			case AnimationKeys.Default: _animator.SetTrigger(GhostEffects.AnimatorKeys.Trigger_Default); break;
			case AnimationKeys.Grab: _animator.SetTrigger(GhostEffects.AnimatorKeys.Trigger_Grab); break;
			case AnimationKeys.SnapNeck: _animator.SetTrigger(GhostEffects.AnimatorKeys.Trigger_SnapNeck); break;
			case AnimationKeys.CallForHelp: _animator.SetTrigger(GhostEffects.AnimatorKeys.Trigger_CallForHelp); break;
		};
	}

	public void ToggleWalk(bool move, bool forceUpdate = false)
	{
		if (!forceUpdate && move == _moving)
		{
			return;
		}

		if (move)
		{
			_animator.SetInteger(GhostEffects.AnimatorKeys.Int_MoveStyle, (int)GhostEffects.MovementStyle.Normal);
		}
		else
		{
			_animator.SetInteger(GhostEffects.AnimatorKeys.Int_MoveStyle, (int)GhostEffects.MovementStyle.Stalk);
		}

		_moving = move;
	}

	public void UpdateEffects()
	{
		Vector3 relativeVelocity = _controller.GetRelativeVelocity();
		float speed = 2f;

		Vector2 targetValue = new Vector2(relativeVelocity.x / speed, relativeVelocity.z / speed);
		_smoothedMoveSpeed = _moveSpeedSpring.Update(_smoothedMoveSpeed, targetValue, Time.deltaTime);
		_animator.SetFloat(GhostEffects.AnimatorKeys.Float_MoveDirectionX, _smoothedMoveSpeed.x);
		_animator.SetFloat(GhostEffects.AnimatorKeys.Float_MoveDirectionY, _smoothedMoveSpeed.y);
		_smoothedTurnSpeed = _turnSpeedSpring.Update(_smoothedTurnSpeed, _controller.GetAngularVelocity() / 90f, Time.deltaTime);
		_animator.SetFloat(GhostEffects.AnimatorKeys.Float_TurnSpeed, _smoothedTurnSpeed);

		ToggleWalk(relativeVelocity.ApproxEquals(Vector3.zero));
	}

	public void OnTeleport() 
	{
		PlayOneShot(AudioType.Ghost_Identify_Curious, 1f, UnityEngine.Random.Range(0.9f, 1.1f));
	}

	public void PlayOneShot(AudioType type, float volume = 1f, float pitch = 1f)
	{
		_oneShotAudioSource.pitch = pitch;
		_oneShotAudioSource.PlayOneShot(type, volume);
	}

	private void Anim_SnapNeck() =>
		SnapNeck?.Invoke();

	private void Anim_SnapNeck_Audio()
	{
		PlayOneShot(AudioType.DBAnglerfishDetectDisturbance, 1f, 1.2f);
		Locator.GetPlayerAudioController().PlayOneShotInternal(AudioType.Ghost_NeckSnap);
		RumbleManager.PlayGhostNeckSnap();
	}

	private void Anim_LiftPlayer() =>
		LiftPlayer?.Invoke();

	private void Anim_LiftPlayer_Audio()
	{
		PlayOneShot(AudioType.GhostSequence_Fear_Slam, 1f, 1f);
		Locator.GetPlayerAudioController().PlayOneShotInternal(AudioType.Death_Crushed);
	}

	private void Anim_CallForHelp()
	{
		PlayOneShot(AudioType.DBAnglerfishDetectTarget, 1f, 1.2f);
	}
}
