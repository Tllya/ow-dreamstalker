﻿using Dreamstalker.Handlers.SolarSystem;
using Dreamstalker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TimelineObliterationController;

namespace Dreamstalker.Components;

internal class DreamstalkerController : VisibilityObject
{
	private DreamstalkerEffectsController _effects;
	private DreamstalkerGrabController _grabber;

	private float _angularVelocity;
	private float _angularAcceleration = 360f;

	private Vector3 _velocity = Vector3.zero;
	private float _acceleration = 10f;

	private Vector3 _localTargetPosition = Vector3.zero;

	private Collider _playerCollider;
	private Collider _dreamstalkerCollider;

	private float _lastTeleportTime;
	public float teleportCooldown = 10f;

	private Transform _relativeTransform;

	private bool _stalking;

	public float farDistance = 30f;
	public float nearDistance = 10f;
	public float grabDistance = 2f;
	public float maxSpeed = 4f;

	public override void Awake()
	{
		base.Awake();

		_effects = GetComponent<DreamstalkerEffectsController>();
		_grabber = GetComponentInChildren<DreamstalkerGrabController>();

		_dreamstalkerCollider = gameObject.AddComponent<CapsuleCollider>();
		gameObject.AddComponent<OWCollider>();

		var visibilityTracker = new GameObject("VisibilityTracker_Sphere");
		visibilityTracker.transform.parent = transform;
		visibilityTracker.transform.localPosition = Vector3.zero;
		var sphere = visibilityTracker.AddComponent<SphereShape>();
		sphere.radius = 4f;
		var tracker = visibilityTracker.AddComponent<ShapeVisibilityTracker>();
		_visibilityTrackers = new VisibilityTracker[] { tracker };
	}

	public void Start()
	{
		_playerCollider = Locator.GetPlayerCollider();

		_stalking = false;

		PropHandler.Instance.CampfireLit.AddListener(StartStalking);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		PropHandler.Instance.CampfireLit.RemoveListener(StartStalking);
	}

	public void SetPlanet(AstroObject planet)
	{
		SetSector(planet.GetRootSector());
		_relativeTransform = planet.transform;
	}

	private void TurnTowardsLocalDirection(Vector3 localDirection, float targetDegreesPerSecond)
	{
		var up = LocalUp();

		var from = Vector3.ProjectOnPlane(WorldToLocalDirection(transform.forward), up);
		var to = Vector3.ProjectOnPlane(localDirection, up);
		var angleDiff = OWMath.Angle(from, to, up);

		var direction = Mathf.Sign(angleDiff);
		var deltaAngle = _angularVelocity * _angularVelocity / (2f * _angularAcceleration);
		var target = targetDegreesPerSecond * direction;
		if ((direction > 0f && angleDiff <= deltaAngle) || (direction < 0f && angleDiff >= -deltaAngle))
		{
			target = 0f;
		}
		_angularVelocity = Mathf.MoveTowards(_angularVelocity, target, _angularAcceleration * Time.fixedDeltaTime);
		var angleToRotate = _angularVelocity * Time.fixedDeltaTime;
		if ((direction > 0f && angleToRotate > angleDiff) || (direction < 0f && angleToRotate < angleDiff))
		{
			angleToRotate = angleDiff;
		}
		Quaternion localRotation = Quaternion.AngleAxis(angleToRotate, up) * transform.localRotation;
		transform.localRotation = localRotation;

		transform.LookAt(transform.position + Vector3.ProjectOnPlane(transform.forward, GlobalUp()), GlobalUp());
	}

	public Vector3 GetRelativeVelocity() =>
		transform.InverseTransformDirection(LocalToWorldDirection(_velocity));

	public float GetAngularVelocity() => 
		_angularVelocity;

	public Vector3 WorldToLocalDirection(Vector3 worldDir) =>
		_relativeTransform.InverseTransformDirection(worldDir);

	public Vector3 LocalToWorldDirection(Vector3 localDir) =>
		_relativeTransform.TransformDirection(localDir);

	public Vector3 WorldToLocalPosition(Vector3 worldPos) =>
		_relativeTransform.InverseTransformPoint(worldPos);

	public Quaternion WorldToLocalRotation(Quaternion worldRot) =>
		_relativeTransform.InverseTransformRotation(worldRot);

	public Vector3 LocalUp() =>
		WorldToLocalDirection(GlobalUp());

	public Vector3 GlobalUp() =>
		(transform.position - _relativeTransform.position).normalized;

	private void UpdatePositionFromVelocity()
	{
		Vector3 newPos = transform.localPosition + _velocity * Time.fixedDeltaTime;

		var localPlayerPos = WorldToLocalPosition(Locator.GetPlayerTransform().position);
		var localPlayerRot = WorldToLocalRotation(Locator.GetPlayerTransform().rotation);

		if (Physics.ComputePenetration(_dreamstalkerCollider, newPos, transform.localRotation, _playerCollider, localPlayerPos, localPlayerRot,
			out var direction, out var distance))
		{
			newPos += direction * distance;
		}

		transform.localPosition = newPos;

		// Stick to the ground
		_dreamstalkerCollider.enabled = false;
		var origin = transform.position + GlobalUp() * 2f;
		var originalPosition = transform.position;
		var downwards = -GlobalUp();

		if (Physics.Raycast(origin, downwards, out var hitInfo, 500f, OWLayerMask.physicalMask))
		{
			transform.position = hitInfo.point;
		}
		else if (Physics.Raycast(transform.position + GlobalUp() * 50f, downwards, out hitInfo, 500f, OWLayerMask.physicalMask))
		{
			transform.position = hitInfo.point;
		}

		// Treat it as a teleport if it was a big jump
		if ((transform.position - originalPosition).sqrMagnitude > 1)
		{
			_effects.OnTeleport();
		}

		_dreamstalkerCollider.enabled = true;
	}

	private void TeleportNearPlayer()
	{
		_lastTeleportTime = Time.time;

		_effects.OnTeleport();
		_effects.PlayAnimation(DreamstalkerEffectsController.AnimationKeys.CallForHelp);

		var playerLocalPosition = WorldToLocalPosition(_playerCollider.transform.position);
		var planeVector = Quaternion.FromToRotation(Vector3.up, playerLocalPosition.normalized) * Vector3.left;

		var playerRelativePos = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), playerLocalPosition.normalized) * planeVector;

		transform.localPosition = playerLocalPosition + playerRelativePos * nearDistance;
	}

	private void StalkPlayer()
	{
		var displacement = (_localTargetPosition - transform.localPosition);
		var direction = displacement.normalized;
		var distance = displacement.magnitude;

		if (distance > farDistance && Time.time > _lastTeleportTime + teleportCooldown)
		{
			TeleportNearPlayer();
			return;
		}

		float speed = _velocity.magnitude;
		float deltaPos = speed * speed / (maxSpeed * _acceleration);

		if (distance > deltaPos)
		{
			var target = direction * maxSpeed;
			_velocity = Vector3.MoveTowards(_velocity, target, _acceleration * Time.fixedDeltaTime);
		}
		else
		{
			_velocity = Vector3.MoveTowards(_velocity, Vector3.zero, _acceleration * Time.fixedDeltaTime);
		}

		UpdatePositionFromVelocity();
	}

	public void StartStalking()
	{
		_effects.PlayAnimation(DreamstalkerEffectsController.AnimationKeys.CallForHelp);
		_effects.CallForHelpComplete.AddListener(OnCallForHelpComplete);
	}

	private void OnCallForHelpComplete()
	{
		_effects.CallForHelpComplete.RemoveListener(OnCallForHelpComplete);
		_stalking = true;
	}

	public void StopStalking()
	{
		_stalking = false;
		_velocity = Vector3.zero;
	}

	public float LineOfSightFraction()
	{
		var camera = Locator.GetActiveCamera().transform;
		var direction = (transform.position - camera.position).normalized;

		if (DSMath.VectorApproxEquals(camera.forward, direction)) return 1f;

		var up = Vector3.Cross(camera.forward, direction);
		var angle = OWMath.Angle(camera.forward, direction, up);

		return Mathf.Clamp01(1f - (angle / 90f));
	}

	public void FixedUpdate()
	{
		_localTargetPosition = _relativeTransform.InverseTransformPoint(Locator.GetPlayerTransform().position);

		var localDisplacement = _localTargetPosition - transform.localPosition;
		var localDirection = localDisplacement.normalized;

		TurnTowardsLocalDirection(localDirection, 90f);

		// Move towards player
		if (_stalking)
		{
			StalkPlayer();
		}

		var distance = (Locator.GetPlayerTransform().position - transform.position).magnitude;

		// Only kill player when in stalking mode
		if (_stalking && distance < grabDistance)
		{
			_grabber.GrabPlayer(4f);
			StopStalking();
		}

		var flickerIntensity = Mathf.Clamp01(1f - (distance / 20f));
		PlayerEffectController.Instance.SetStatic(flickerIntensity);
		PlayerEffectController.Instance.SetFlicker(IsVisible() ? 6 * flickerIntensity * LineOfSightFraction() : 0f);

		_effects.UpdateEffects();
	}
}
