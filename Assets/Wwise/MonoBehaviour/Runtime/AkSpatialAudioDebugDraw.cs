#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
[UnityEngine.AddComponentMenu("Wwise/Spatial Audio/AkSpatialAudioDebugDraw")]
[UnityEngine.RequireComponent(typeof(AkGameObj))]
///@brief Add this script on a GameObject to print Spatial Audio paths.
public class AkSpatialAudioDebugDraw : UnityEngine.MonoBehaviour
{
#if UNITY_EDITOR
	/// This allows you to visualize first order reflection sound paths.
	public bool drawFirstOrderReflections = false;

	/// This allows you to visualize second order reflection sound paths.
	public bool drawSecondOrderReflections = false;

	/// This allows you to visualize third or higher order reflection sound paths.
	public bool drawHigherOrderReflections = false;

	/// This allows you to visualize geometric diffraction sound paths between an obstructed emitter and the listener.
	public bool drawDiffractionPaths = false;

	private void OnDrawGizmos()
	{
		if (!UnityEngine.Application.isPlaying || !AkSoundEngine.IsInitialized())
			return;

		if (debugDrawData == null)
			debugDrawData = new DebugDrawData();

		if (drawFirstOrderReflections || drawSecondOrderReflections || drawHigherOrderReflections)
			debugDrawData.DebugDrawEarlyReflections(gameObject, drawFirstOrderReflections, drawSecondOrderReflections, drawHigherOrderReflections);

		if (drawDiffractionPaths)
			debugDrawData.DebugDrawDiffraction(gameObject);
	}

	private class DebugDrawData
	{
		// Constants
		private const uint kMaxIndirectPaths = 64;
		private const uint kMaxDiffractionPaths = 16;
		private readonly UnityEngine.Color32 colorLightYellow = new UnityEngine.Color32(255, 255, 121, 255);
		private readonly UnityEngine.Color32 colorDarkYellow = new UnityEngine.Color32(164, 164, 0, 255);
		private readonly UnityEngine.Color32 colorLightOrange = new UnityEngine.Color32(255, 202, 79, 255);
		private readonly UnityEngine.Color32 colorDarkOrange = new UnityEngine.Color32(164, 115, 0, 255);
		private readonly UnityEngine.Color32 colorLightRed = new UnityEngine.Color32(252, 177, 162, 255);
		private readonly UnityEngine.Color32 colorDarkRed = new UnityEngine.Color32(169, 62, 39, 255);
		private readonly UnityEngine.Color32 colorLightGrey = new UnityEngine.Color32(75, 75, 75, 255);
		private readonly UnityEngine.Color32 colorGreen = new UnityEngine.Color32(38, 113, 88, 255);
		private const float radiusSphere = 0.25f;

		// Calculated path info
		private readonly AkReflectionPathInfoArray indirectPathInfoArray = new AkReflectionPathInfoArray((int)kMaxIndirectPaths);
		private readonly AkDiffractionPathInfoArray diffractionPathInfoArray = new AkDiffractionPathInfoArray((int)kMaxDiffractionPaths);

		public void DebugDrawEarlyReflections(UnityEngine.GameObject gameObject, bool firstOrder, bool secondOrder, bool higherOrder)
		{
			var listenerPosition = UnityEngine.Vector3.zero;
			var emitterPosition = UnityEngine.Vector3.zero;
			uint numValidPaths = (uint)indirectPathInfoArray.Count();
			if (AkSoundEngine.QueryReflectionPaths(gameObject, 0, ref listenerPosition, ref emitterPosition, indirectPathInfoArray, out numValidPaths) != AKRESULT.AK_Success)
				return;

			for (var idxPath = (int)numValidPaths - 1; idxPath >= 0; --idxPath)
			{
				var path = indirectPathInfoArray[idxPath];
				var order = path.numReflections;

				var colorLight = colorLightRed;
				var colorDark = colorDarkRed;

				if (order == 1)
				{
					if (!firstOrder)
						continue;

					colorLight = colorLightYellow;
					colorDark = colorDarkYellow;
				}
				else if (order == 2)
				{
					if (!secondOrder)
						continue;

					colorLight = colorLightOrange;
					colorDark = colorDarkOrange;
				}
				else if (order > 2 && !higherOrder)
					continue;

				var listenerPt = listenerPosition;

				for (var idxSeg = (int)path.numPathPoints - 1; idxSeg >= 0; --idxSeg)
				{
					var pt = path.GetPathPoint((uint)idxSeg);

					UnityEngine.Debug.DrawLine(listenerPt, pt, path.isOccluded ? colorLightGrey : colorLight);

					UnityEngine.Gizmos.color = path.isOccluded ? colorLightGrey : colorLight;
					UnityEngine.Gizmos.DrawWireSphere(pt, radiusSphere / 2 / order);

					if (!path.isOccluded)
					{
						var surface = path.GetAcousticSurface((uint)idxSeg);
						DrawLabelInFrontOfCam(pt, surface.strName, 100000, colorDark);
					}

					float dfrnAmount = path.GetDiffraction((uint)idxSeg);
					if (dfrnAmount > 0)
					{
						string dfrnAmountStr = dfrnAmount.ToString("0.#%");
						DrawLabelInFrontOfCam(pt, dfrnAmountStr, 100000, colorDark);
					}

					listenerPt = pt;
				}

				if (!path.isOccluded)
				{
					// Finally the last path segment towards the emitter.
					UnityEngine.Debug.DrawLine(listenerPt, emitterPosition, path.isOccluded ? colorLightGrey : colorLight);
				}
			}
		}

		public void DebugDrawDiffraction(UnityEngine.GameObject gameObject)
		{
			var listenerPosition = UnityEngine.Vector3.zero;
			var emitterPosition = UnityEngine.Vector3.zero;
			uint numValidPaths = (uint)diffractionPathInfoArray.Count();
			if (AkSoundEngine.QueryDiffractionPaths(gameObject, 0, ref listenerPosition, ref emitterPosition, diffractionPathInfoArray, out numValidPaths) != AKRESULT.AK_Success)
				return;

			for (var idxPath = (int)numValidPaths - 1; idxPath >= 0; --idxPath)
			{
				var path = diffractionPathInfoArray[idxPath];
				if (path.nodeCount <= 0)
					continue;

				var prevPt = listenerPosition;

				for (var idxSeg = 0; idxSeg < (int)path.nodeCount; ++idxSeg)
				{
					var pt = path.GetNodes((uint)idxSeg);
					UnityEngine.Debug.DrawLine(prevPt, pt, colorGreen);

					float angle = path.GetAngles((uint)idxSeg) / UnityEngine.Mathf.PI;
					if (angle > 0)
					{
						string angleStr = angle.ToString("0.#%");
						DrawLabelInFrontOfCam(pt, angleStr, 100000, colorGreen);
					}

					prevPt = pt;
				}

				UnityEngine.Debug.DrawLine(prevPt, emitterPosition, colorGreen);
			}
		}
	}

	private static DebugDrawData debugDrawData = null;

	private static void DrawLabelInFrontOfCam(UnityEngine.Vector3 position, string name, float distance, UnityEngine.Color c)
	{
		var style = new UnityEngine.GUIStyle();
		var oncam = UnityEngine.Camera.current.WorldToScreenPoint(position);

		if (oncam.x >= 0 && oncam.x <= UnityEngine.Camera.current.pixelWidth && oncam.y >= 0 &&
			oncam.y <= UnityEngine.Camera.current.pixelHeight && oncam.z > 0 && oncam.z < distance)
		{
			style.normal.textColor = c;
			UnityEditor.Handles.Label(position, name, style);
		}
	}
#endif
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.