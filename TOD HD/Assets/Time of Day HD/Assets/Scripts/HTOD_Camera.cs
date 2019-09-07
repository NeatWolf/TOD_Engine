﻿using UnityEngine;

/// Sky dome management camera component.
///
/// Move and scale the sky dome every frame after the rest of the scene has fully updated.

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Time of Day/Camera Main Script")]
public class HTOD_Camera : MonoBehaviour
{
	/// Sky dome reference inspector variable.
	/// Will automatically be searched in the scene if not set in the inspector.
	public HTOD_Sky sky;

	/// Automatically move the sky dome to the camera position in OnPreCull().
	public bool DomePosToCamera = true;

	/// The sky dome position offset relative to the camera.
	public Vector3 DomePosOffset = Vector3.zero;

	/// Automatically scale the sky dome to the camera far clip plane in OnPreCull().
	public bool DomeScaleToFarClip = true;

	/// The sky dome scale factor relative to the camera far clip plane.
	public float DomeScaleFactor = 0.95f;

	public bool HDR
	{
		get
		{
			return cameraComponent ? cameraComponent.allowHDR : false;
		}
	}

	public float NearClipPlane
	{
		get
		{
			return cameraComponent ? cameraComponent.nearClipPlane : 0.1f;
		}
	}

	public float FarClipPlane
	{
		get
		{
			return cameraComponent ? cameraComponent.farClipPlane : 1000f;
		}
	}

	public Color BackgroundColor
	{
		get
		{
			return cameraComponent ? cameraComponent.backgroundColor : Color.black;
		}
	}

	private Camera cameraComponent = null;
	private Transform cameraTransform = null;

	protected void OnValidate()
	{
		DomeScaleFactor = Mathf.Clamp(DomeScaleFactor, 0.01f, 1.0f);
	}

	protected void OnEnable()
	{
		cameraComponent = GetComponent<Camera>();
		cameraTransform = GetComponent<Transform>();

		if (!sky) sky = FindSky(true);
	}

	protected void Update()
	{
		if (!sky) sky = FindSky();
		if (!sky || !sky.Initialized) return;

		sky.Components.Camera = this;

		if (cameraComponent.clearFlags != CameraClearFlags.SolidColor)
		{
			cameraComponent.clearFlags = CameraClearFlags.SolidColor;
		}

		if (cameraComponent.backgroundColor != Color.clear)
		{
			cameraComponent.backgroundColor = Color.clear;
		}

		if (RenderSettings.skybox != sky.Resources.Skybox)
		{
			RenderSettings.skybox = sky.Resources.Skybox;

			DynamicGI.UpdateEnvironment();
		}
	}

	protected void OnPreCull()
	{
		if (!sky) sky = FindSky();
		if (!sky || !sky.Initialized) return;

		if (DomeScaleToFarClip) DoDomeScaleToFarClip();

		if (DomePosToCamera) DoDomePosToCamera();
	}

	private HTOD_Sky FindSky(bool fallback = false)
	{
		if (HTOD_Sky.Instance) return HTOD_Sky.Instance;
		if (fallback) return FindObjectOfType(typeof(HTOD_Sky)) as HTOD_Sky;
		return null;
	}

	public void DoDomeScaleToFarClip()
	{
		float size = DomeScaleFactor * cameraComponent.farClipPlane;
		var localScale = new Vector3(size, size, size);

		#if UNITY_EDITOR
		if (sky.Components.DomeTransform.localScale != localScale)
		#endif
		{
			sky.Components.DomeTransform.localScale = localScale;
		}
	}

	public void DoDomePosToCamera()
	{
		var position = cameraTransform.position + cameraTransform.rotation * DomePosOffset;

		#if UNITY_EDITOR
		if (sky.Components.DomeTransform.position != position)
		#endif
		{
			sky.Components.DomeTransform.position = position;
		}
	}
}