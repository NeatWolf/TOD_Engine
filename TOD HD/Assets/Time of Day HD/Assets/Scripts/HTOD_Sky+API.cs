﻿using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public partial class HTOD_Sky : MonoBehaviour
{
	private static List<HTOD_Sky> instances = new List<HTOD_Sky>();

	private int probeRenderID = -1;

	//
	// Static properties
	//

	/// All currently active sky dome instances.
	public static List<HTOD_Sky> Instances
	{
		get
		{
			return instances;
		}
	}

	/// The most recently created sky dome instance.
	public static HTOD_Sky Instance
	{
		get
		{
			return instances.Count == 0 ? null : instances[instances.Count-1];
		}
	}

	//
	// Inspector variables
	//

	/// Auto: Use the player settings.
	/// Linear: Force linear color space.
	/// Gamma: Force gamma color space.
	[Tooltip("Auto: Use the player settings.\nLinear: Force linear color space.\nGamma: Force gamma color space.")]
	public HTOD_ColorSpaceType ColorSpace = HTOD_ColorSpaceType.Auto;

	/// Auto: Use the camera settings.
	/// HDR: Force high dynamic range.
	/// LDR: Force low dynamic range.
	[Tooltip("Auto: Use the camera settings.\nHDR: Force high dynamic range.\nLDR: Force low dynamic range.")]
	public HTOD_ColorRangeType ColorRange = HTOD_ColorRangeType.Auto;

	/// Raw: Write color without modifications.
	/// Dithered: Add dithering to reduce banding.
	[Tooltip("Raw: Write color without modifications.\nDithered: Add dithering to reduce banding.")]
	public HTOD_ColorOutputType ColorOutput = HTOD_ColorOutputType.Dithered;

	/// Per Vertex: Calculate sky color per vertex.
	/// Per Pixel: Calculate sky color per pixel.
	[Tooltip("Per Vertex: Calculate sky color per vertex.\nPer Pixel: Calculate sky color per pixel.")]
	public HTOD_SkyQualityType SkyQuality = HTOD_SkyQualityType.PerVertex;

	/// Low: Only recommended for very old mobile devices.
	/// Medium: Simplified cloud shading.
	/// High: Physically based cloud shading.
	[Tooltip("Low: Only recommended for very old mobile devices.\nMedium: Simplified cloud shading.\nHigh: Physically based cloud shading.")]
	public HTOD_CloudQualityType CloudQuality = HTOD_CloudQualityType.High;

	/// Low: Only recommended for very old mobile devices.
	/// Medium: Simplified mesh geometry.
	/// High: Detailed mesh geometry.
	[Tooltip("Low: Only recommended for very old mobile devices.\nMedium: Simplified mesh geometry.\nHigh: Detailed mesh geometry.")]
	public HTOD_MeshQualityType MeshQuality = HTOD_MeshQualityType.High;

	/// Low: Recommended for most mobile devices.
	/// Medium: Includes most visible stars.
	/// High: Includes all visible stars.
	[Tooltip("Low: Recommended for most mobile devices.\nMedium: Includes most visible stars.\nHigh: Includes all visible stars.")]
	public HTOD_StarQualityType StarQuality = HTOD_StarQualityType.High;

	/// Parameters of the day and night cycle.
	public HTOD_CycleParameters Cycle;

	/// Parameters of the world.
	public HTOD_WorldParameters World;

	/// Parameters of the atmosphere.
	public HTOD_AtmosphereParameters Atmosphere;

	/// Parameters of the day.
	public HTOD_DayParameters Day;

	/// Parameters of the night.
	public HTOD_NightParameters Night;

	/// Parameters of the sun.
	public HTOD_SunParameters Sun;

	/// Parameters of the moon.
	public HTOD_MoonParameters Moon;

	/// Parameters of the stars.
	public HTOD_StarParameters Stars;

	/// Parameters of the cloud layers.
	public HTOD_CloudParameters Clouds;

	/// Parameters of the light source.
	public HTOD_LightParameters Light;

	/// Parameters of the fog.
	public HTOD_FogParameters Fog;

	/// Parameters of the ambient light.
	public HTOD_AmbientParameters Ambient;

	/// Parameters of the reflection cubemap.
	public HTOD_ReflectionParameters Reflection;

	//
	// Class properties
	//

	/// Whether or not the sky dome was successfully initialized.
	public bool Initialized
	{
		get; private set;
	}

	/// Whether or not the sky dome is running in headless mode.
	public bool Headless
	{
		#if UNITY_EDITOR
		get { return false; }
		#else
		get { return Camera.allCamerasCount == 0; }
		#endif
	}

	/// Containins references to all components.
	public HTOD_Components Components
	{
		get; private set;
	}

	/// Containins references to all resources.
	public HTOD_Resources Resources
	{
		get; private set;
	}

	/// Boolean to check if it is day.
	public bool IsDay
	{
		get; private set;
	}

	/// Boolean to check if it is night.
	public bool IsNight
	{
		get; private set;
	}

	/// Radius of the sky dome.
	public float Radius
	{
		get { return Components.DomeTransform.lossyScale.y; }
	}

	/// Diameter of the sky dome.
	public float Diameter
	{
		get { return Components.DomeTransform.lossyScale.y * 2; }
	}

	/// Falls off the darker the sunlight gets.
	/// Can for example be used to lerp between day and night values in shaders.
	/// \n = +1 at day
	/// \n = 0 at night
	public float LerpValue
	{
		get; private set;
	}

	/// Sun zenith angle in degrees.
	/// \n = 0   if the sun is exactly at zenith.
	/// \n = 90  if the sun is exactly at the horizon.
	/// \n = 180 if the sun is exactly opposize to zenith.
	public float SunZenith
	{
		get; private set;
	}

	/// Sun altitude angle in degrees.
	/// \n = -90 if the sun is exactly opposite to zenith.
	/// \n = 0   if the sun is exactly at the horizon.
	/// \n = 90  if the sun is exactly at zenith.
	public float SunAltitude
	{
		get; private set;
	}

	/// Sun azimuth angle in degrees.
	/// \n = 0   if the sun is exactly in the north.
	/// \n = 90  if the sun is exactly in the east.
	/// \n = 180 if the sun is exactly in the south.
	/// \n = 270 if the sun is exactly in the west.
	public float SunAzimuth
	{
		get; private set;
	}

	/// Moon zenith angle in degrees.
	/// \n = 0   if the moon is exactly at zenith.
	/// \n = 180 if the moon is exactly below the ground.
	public float MoonZenith
	{
		get; private set;
	}

	/// Moon altitude angle in degrees.
	/// \n = -90 if the moon is exactly opposite to zenith.
	/// \n = 0   if the moon is exactly at the horizon.
	/// \n = 90  if the moon is exactly at zenith.
	public float MoonAltitude
	{
		get; private set;
	}

	/// Moon azimuth angle in degrees.
	/// \n = 0   if the moon is exactly in the north.
	/// \n = 90  if the moon is exactly in the east.
	/// \n = 180 if the moon is exactly in the south.
	/// \n = 270 if the moon is exactly in the west.
	public float MoonAzimuth
	{
		get; private set;
	}

	/// Time the sun sets.
	public float SunsetTime
	{
		get; private set;
	}

	/// Time the sun rises.
	public float SunriseTime
	{
		get; private set;
	}

	/// The current local sidereal time.
	public float LocalSiderealTime
	{
		get; private set;
	}

	/// Currently active light source zenith angle in degrees.
	/// \n = 0  if the currently active light source (sun or moon) is exactly at zenith.
	/// \n = 90 if the currently active light source (sun or moon) is exactly at the horizon.
	public float LightZenith
	{
		get { return Mathf.Min(SunZenith, MoonZenith); }
	}

	/// Current light intensity.
	public float LightIntensity
	{
		get { return Components.LightSource.intensity; }
	}

	/// Current sun visibility.
	public float SunVisibility
	{
		get; private set;
	}

	/// Current moon visibility.
	public float MoonVisibility
	{
		get; private set;
	}

	/// Sun direction vector in world space.
	public Vector3 SunDirection
	{
		get; private set;
	}

	/// Moon direction vector in world space.
	public Vector3 MoonDirection
	{
		get; private set;
	}

	/// Current directional light vector in world space.
	/// Lerps between HTOD_Sky.SunDirection and HTOD_Sky.MoonDirection at dusk and dawn.
	public Vector3 LightDirection
	{
		get; private set;
	}

	/// Sun direction vector in sky dome object space.
	public Vector3 LocalSunDirection
	{
		get; private set;
	}

	/// Moon direction vector in sky dome object space.
	public Vector3 LocalMoonDirection
	{
		get; private set;
	}

	/// Current directional light vector in sky dome object space.
	/// Lerps between HTOD_Sky.LocalSunDirection and HTOD_Sky.LocalMoonDirection at dusk and dawn.
	public Vector3 LocalLightDirection
	{
		get; private set;
	}

	/// Current sun light color.
	public Color SunLightColor
	{
		get; private set;
	}

	/// Current moon light color.
	public Color MoonLightColor
	{
		get; private set;
	}

	/// Current light color.
	/// The color of HTOD_Sky.Components.LightSource.
	public Color LightColor
	{
		get { return Components.LightSource.color; }
	}

	/// Current sun ray color.
	public Color SunRayColor
	{
		get; private set;
	}

	/// Current moon ray color.
	public Color MoonRayColor
	{
		get; private set;
	}

	/// Current sun sky color.
	public Color SunSkyColor
	{
		get; private set;
	}

	/// Current moon sky color.
	public Color MoonSkyColor
	{
		get; private set;
	}

	/// Current sun mesh color.
	public Color SunMeshColor
	{
		get; private set;
	}

	/// Current moon mesh color.
	public Color MoonMeshColor
	{
		get; private set;
	}

	/// Current sun cloud color.
	public Color SunCloudColor
	{
		get; private set;
	}

	/// Current moon cloud color.
	public Color MoonCloudColor
	{
		get; private set;
	}

	/// Current fog color.
	public Color FogColor
	{
		get; private set;
	}

	/// Current ground color.
	public Color GroundColor
	{
		get; private set;
	}

	/// Current ambient light color.
	public Color AmbientColor
	{
		get; private set;
	}

	/// Current moon halo color.
	public Color MoonHaloColor
	{
		get; private set;
	}

	/// Current reflection probe.
	public ReflectionProbe Probe
	{
		get; private set;
	}

	//
	// Class methods
	//

	/// Convert spherical coordinates to cartesian coordinates.
	/// \param radius Spherical coordinates radius.
	/// \param theta Spherical coordinates theta.
	/// \param phi Spherical coordinates phi.
	/// \return Unity position in world space.
	public Vector3 OrbitalToUnity(float radius, float theta, float phi)
	{
		Vector3 res;

		float sinTheta = Mathf.Sin(theta);
		float cosTheta = Mathf.Cos(theta);
		float sinPhi   = Mathf.Sin(phi);
		float cosPhi   = Mathf.Cos(phi);

		res.z = radius * sinTheta * cosPhi;
		res.y = radius * cosTheta;
		res.x = radius * sinTheta * sinPhi;

		return res;
	}

	/// Convert spherical coordinates to cartesian coordinates.
	/// \param theta Spherical coordinates theta.
	/// \param phi Spherical coordinates phi.
	/// \return Unity position in local space.
	public Vector3 OrbitalToLocal(float theta, float phi)
	{
		Vector3 res;

		float sinTheta = Mathf.Sin(theta);
		float cosTheta = Mathf.Cos(theta);
		float sinPhi   = Mathf.Sin(phi);
		float cosPhi   = Mathf.Cos(phi);

		res.z = sinTheta * cosPhi;
		res.y = cosTheta;
		res.x = sinTheta * sinPhi;

		return res;
	}

	/// Sample atmosphere colors from the sky dome.
	/// \param direction View direction in world space.
	/// \param directLight Whether or not to include direct light.
	/// \return Color of the atmosphere in the specified direction.
	public Color SampleAtmosphere(Vector3 direction, bool directLight = true)
	{
		Vector3 dir = Components.DomeTransform.InverseTransformDirection(direction);

		Color color = ShaderScatteringColor(dir, directLight);
		color = HTOD_HDR2LDR(color);
		color = HTOD_LINEAR2GAMMA(color);

		return color;
	}

	/// Render the sky dome to 3rd order spherical harmonics.
	public SphericalHarmonicsL2 RenderToSphericalHarmonics()
	{
		float saturation = Ambient.Saturation;
		float intensity  = Mathf.Lerp(Night.AmbientMultiplier, Day.AmbientMultiplier, LerpValue);

		return RenderToSphericalHarmonics(intensity, saturation);
	}

	/// Render the sky dome to 3rd order spherical harmonics.
	public SphericalHarmonicsL2 RenderToSphericalHarmonics(float intensity, float saturation)
	{
		var sh = new SphericalHarmonicsL2();

		bool directLight = false;

		const float scale1 = 1f / 7f;
		const float scale2 = 2f / 7f;
		const float scale3 = 3f / 7f;

		var ground = HTOD_Util.AdjustRGB(AmbientColor.linear, intensity, saturation);
		var halfway = new Vector3(0.61237243569579f, 0.5f, 0.61237243569579f);

		// Top
		{
			var dir = Vector3.up;
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale3);
		}

		// Upper
		{
			var dir = new Vector3(-halfway.x, +halfway.y, -halfway.z);
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale2);
		}
		{
			var dir = new Vector3(+halfway.x, +halfway.y, -halfway.z);
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale2);
		}
		{
			var dir = new Vector3(-halfway.x, +halfway.y, +halfway.z);
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale2);
		}
		{
			var dir = new Vector3(+halfway.x, +halfway.y, +halfway.z);
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale2);
		}

		// Equator
		{
			var dir = Vector3.left;
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale1);
		}
		{
			var dir = Vector3.right;
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale1);
		}
		{
			var dir = Vector3.back;
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale1);
		}
		{
			var dir = Vector3.forward;
			var sky = SampleAtmosphere(dir, directLight).linear;
			var col = HTOD_Util.AdjustRGB(sky, intensity, saturation);
			sh.AddDirectionalLight(dir, col, scale1);
		}

		// Lower
		{
			var dir = new Vector3(-halfway.x, -halfway.y, -halfway.z);
			sh.AddDirectionalLight(dir, ground, scale2);
		}
		{
			var dir = new Vector3(+halfway.x, -halfway.y, -halfway.z);
			sh.AddDirectionalLight(dir, ground, scale2);
		}
		{
			var dir = new Vector3(-halfway.x, -halfway.y, +halfway.z);
			sh.AddDirectionalLight(dir, ground, scale2);
		}
		{
			var dir = new Vector3(+halfway.x, -halfway.y, +halfway.z);
			sh.AddDirectionalLight(dir, ground, scale2);
		}

		// Bottom
		{
			var dir = Vector3.down;
			sh.AddDirectionalLight(dir, ground, scale3);
		}

		return sh;
	}

	/// Render the sky dome to a cubemap render texture.
	/// \param targetTexture Target RenderTexture in which rendering should be done.
	public void RenderToCubemap(RenderTexture targetTexture = null)
	{
		if (!Probe)
		{
			Probe = new GameObject().AddComponent<ReflectionProbe>();
			Probe.name = gameObject.name + " Reflection Probe";
			Probe.mode = ReflectionProbeMode.Realtime;
		}

		if (probeRenderID < 0 || Probe.IsFinishedRendering(probeRenderID))
		{
			var size = float.MaxValue;

			Probe.transform.position = Components.DomeTransform.position;
			Probe.size = new Vector3(size, size, size);
			Probe.intensity = RenderSettings.reflectionIntensity;
			Probe.clearFlags = Reflection.ClearFlags;
			Probe.cullingMask = Reflection.CullingMask;
			Probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
			Probe.timeSlicingMode = Reflection.TimeSlicing;
			Probe.resolution = Mathf.ClosestPowerOfTwo(Reflection.Resolution);
			if (Components.Camera != null)
			{
				Probe.backgroundColor = Components.Camera.BackgroundColor;
				Probe.nearClipPlane = Components.Camera.NearClipPlane;
				Probe.farClipPlane = Components.Camera.FarClipPlane;
			}
			probeRenderID = Probe.RenderProbe(targetTexture);
		}
	}

	/// Calculate the fog color.
	/// \param directLight Whether or not to include direct light.
	public Color SampleFogColor(bool directLight = true)
	{
		var camera = Vector3.forward;
		if (Components.Camera != null)
		{
			camera = Quaternion.Euler(0, Components.Camera.transform.rotation.eulerAngles.y, 0) * camera;
		}
		var sample = Vector3.Lerp(camera, Vector3.up, Fog.HeightBias);
		var color  = SampleAtmosphere(sample.normalized, directLight);
		return new Color(color.r, color.g, color.b, 1);
	}

	/// Calculate the sky color.
	public Color SampleSkyColor()
	{
		var sample = SunDirection; sample.y = Mathf.Abs(sample.y);
		var color  = SampleAtmosphere(sample.normalized, false);
		return new Color(color.r, color.g, color.b, 1);
	}

	/// Calculate the equator color.
	public Color SampleEquatorColor()
	{
		var sample = SunDirection; sample.y = 0;
		var color  = SampleAtmosphere(sample.normalized, false);
		return new Color(color.r, color.g, color.b, 1);
	}

	/// Update the RenderSettings fog color according to HTOD_FogParameters.
	public void UpdateFog()
	{
		switch (Fog.Mode)
		{
			case HTOD_FogType.None:
				break;

			case HTOD_FogType.Atmosphere:
				var fogColor = SampleFogColor(false);

				#if UNITY_EDITOR
				if (RenderSettings.fogColor != fogColor)
				#endif
				{
					RenderSettings.fogColor = fogColor;
				}
				break;

			case HTOD_FogType.Directional:
				var fogColorDirectional = SampleFogColor(true);

				#if UNITY_EDITOR
				if (RenderSettings.fogColor != fogColorDirectional)
				#endif
				{
					RenderSettings.fogColor = fogColorDirectional;
				}
				break;

			case HTOD_FogType.Gradient:
				#if UNITY_EDITOR
				if (RenderSettings.fogColor != FogColor)
				#endif
				{
					RenderSettings.fogColor = FogColor;
				}
				break;
		}
	}

	/// Update the RenderSettings ambient light according to HTOD_AmbientParameters.
	public void UpdateAmbient()
	{
		float saturation = Ambient.Saturation;
		float intensity  = Mathf.Lerp(Night.AmbientMultiplier, Day.AmbientMultiplier, LerpValue);

		switch (Ambient.Mode)
		{
			case HTOD_AmbientType.Color:
				var ambientColor = HTOD_Util.AdjustRGB(AmbientColor, intensity, saturation);

				#if UNITY_EDITOR
				if (RenderSettings.ambientMode != AmbientMode.Flat)
				#endif
				{
					RenderSettings.ambientMode = AmbientMode.Flat;
				}

				#if UNITY_EDITOR
				if (RenderSettings.ambientLight != ambientColor)
				#endif
				{
					RenderSettings.ambientLight = ambientColor;
				}

				#if UNITY_EDITOR
				if (RenderSettings.ambientIntensity != intensity)
				#endif
				{
					RenderSettings.ambientIntensity = intensity;
				}
				break;

			case HTOD_AmbientType.Gradient:
				var groundColor  = HTOD_Util.AdjustRGB(AmbientColor, intensity, saturation);
				var equatorColor = HTOD_Util.AdjustRGB(SampleEquatorColor(), intensity, saturation);
				var skyColor     = HTOD_Util.AdjustRGB(SampleSkyColor(), intensity, saturation);

				#if UNITY_EDITOR
				if (RenderSettings.ambientMode != AmbientMode.Trilight)
				#endif
				{
					RenderSettings.ambientMode = AmbientMode.Trilight;
				}

				#if UNITY_EDITOR
				if (RenderSettings.ambientSkyColor != skyColor)
				#endif
				{
					RenderSettings.ambientSkyColor = skyColor;
				}

				#if UNITY_EDITOR
				if (RenderSettings.ambientEquatorColor != equatorColor)
				#endif
				{
					RenderSettings.ambientEquatorColor = equatorColor;
				}

				#if UNITY_EDITOR
				if (RenderSettings.ambientGroundColor != groundColor)
				#endif
				{
					RenderSettings.ambientGroundColor = groundColor;
				}

				#if UNITY_EDITOR
				if (RenderSettings.ambientIntensity != intensity)
				#endif
				{
					RenderSettings.ambientIntensity = intensity;
				}
				break;

			case HTOD_AmbientType.Spherical:
				var fallbackColor = HTOD_Util.AdjustRGB(AmbientColor, intensity, saturation);

				#if UNITY_EDITOR
				if (RenderSettings.ambientMode != AmbientMode.Skybox)
				#endif
				{
					RenderSettings.ambientMode = AmbientMode.Skybox;
				}

				#if UNITY_EDITOR
				if (RenderSettings.ambientLight != fallbackColor)
				#endif
				{
					RenderSettings.ambientLight = fallbackColor;
				}

				#if UNITY_EDITOR
				if (RenderSettings.ambientIntensity != intensity)
				#endif
				{
					RenderSettings.ambientIntensity = intensity;
				}

				RenderSettings.ambientProbe = RenderToSphericalHarmonics(intensity, saturation);
				break;
		}
	}

	/// Update the RenderSettings reflection probe according to HTOD_ReflectionParameters.
	public void UpdateReflection()
	{
		switch (Reflection.Mode)
		{
			case HTOD_ReflectionType.Cubemap:
				float intensity = Mathf.Lerp(Night.ReflectionMultiplier, Day.ReflectionMultiplier, LerpValue);

				#if UNITY_EDITOR
				if (RenderSettings.defaultReflectionMode != DefaultReflectionMode.Skybox)
				#endif
				{
					RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
				}

				#if UNITY_EDITOR
				if (RenderSettings.reflectionIntensity != intensity)
				#endif
				{
					RenderSettings.reflectionIntensity = intensity;
				}

				if (Application.isPlaying)
				{
					RenderToCubemap();
				}
				break;
		}
	}

	/// Load parameters at runtime.
	/// \param xml The parameters to load, serialized to XML.
	public void LoadParameters(string xml)
	{
		using (var stringReader = new System.IO.StringReader(xml))
		{
			using (var textReader = new System.Xml.XmlTextReader(stringReader))
			{
				var serializer = new System.Xml.Serialization.XmlSerializer(typeof(HTOD_Parameters));
				var parameters = serializer.Deserialize(textReader) as HTOD_Parameters;

				parameters.ToSky(this);
			}
		}
	}

	/// Save parameters at runtime.
	/// \return The parameters serialized to XML.
	public string SaveParameters()
	{
		var builder = new System.Text.StringBuilder();

		using (var stringWriter = new System.IO.StringWriter(builder))
		{
			using (var textWriter = new System.Xml.XmlTextWriter(stringWriter))
			{
				textWriter.Formatting = System.Xml.Formatting.Indented;

				var serializer = new System.Xml.Serialization.XmlSerializer(typeof(HTOD_Parameters));
				var parameters = new HTOD_Parameters(this);

				serializer.Serialize(textWriter, parameters);
			}
		}

		return builder.ToString();
	}
}
