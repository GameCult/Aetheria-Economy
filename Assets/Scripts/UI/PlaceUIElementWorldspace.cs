using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using static Noise1D;

/// <summary>
/// Place an UI element to a world position
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PlaceUIElementWorldspace : MonoBehaviour
{
    public float NoiseAmplitude;
    public float NoiseFrequency;
    public RectTransform CanvasRectTransform;
    public float BorderPixels = 10;

    public Vector3 Target;

    private RectTransform _rectTransform;
    private Transform _cameraTransform;
    private float _noiseOffset;

    void Start()
    {
        // Get the rect transform
        _rectTransform = GetComponent<RectTransform>();
        _cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        _noiseOffset = UnityEngine.Random.Range(-100f, 100f);
    }

    /// <summary>
    /// Move the UI element to the world position
    /// </summary>
    void LateUpdate()
    {
        if (Target == null)
            return;

        _noiseOffset += Time.deltaTime * NoiseFrequency;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, Target);
        screenPoint *= Mathf.Sign(Vector3.Dot(_cameraTransform.forward, Target - _cameraTransform.position));
        screenPoint += new Vector2(noise(_noiseOffset), noise(10 + _noiseOffset)) * NoiseAmplitude;
        screenPoint = new Vector2(Mathf.Clamp(screenPoint.x, BorderPixels, Screen.width - BorderPixels),Mathf.Clamp(screenPoint.y, BorderPixels, Screen.height - BorderPixels));
        _rectTransform.anchoredPosition = screenPoint - CanvasRectTransform.sizeDelta / 2f;
    }
}