using UnityEngine;

[ExecuteInEditMode]
public class FilterTest : MonoBehaviour
{
    enum DownSampleMode { Off, Half, Quarter }

    [SerializeField, HideInInspector]
    private Shader _shader;

    [SerializeField]
    private DownSampleMode _downSampleMode = DownSampleMode.Quarter;

    [SerializeField, Range(0, 8)]
    private int _iteration = 4;

    private Material _material;

    private int _width;
    private int _height;

    private RenderTexture _target;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source,destination);
        
        if (_material == null)
        {
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.HideAndDontSave;
        }

        if (Screen.width != _width || Screen.height != _height)
        {
            _width = Screen.width;
            _height = Screen.height;
            if (_downSampleMode == DownSampleMode.Half)
                _target = new RenderTexture(_width / 2, _height / 2, 0);
            else if (_downSampleMode == DownSampleMode.Quarter)
                _target = new RenderTexture(_width / 4, _height / 4, 0);
        }

        RenderTexture rt2;

        if (_downSampleMode == DownSampleMode.Half)
        {
            rt2 = RenderTexture.GetTemporary(source.width / 2, source.height / 2);
            Graphics.Blit(source, _target);
        }
        else if (_downSampleMode == DownSampleMode.Quarter)
        {
            rt2 = RenderTexture.GetTemporary(source.width / 4, source.height / 4);
            Graphics.Blit(source, _target, _material, 0);
        }
        else
        {
            rt2 = RenderTexture.GetTemporary(source.width, source.height);
            Graphics.Blit(source, _target);
        }

        for (var i = 0; i < _iteration; i++)
        {
            Graphics.Blit(_target, rt2, _material, 1);
            Graphics.Blit(rt2, _target, _material, 2);
        }

        RenderTexture.ReleaseTemporary(rt2);
        Shader.SetGlobalTexture("_CameraBlur", _target);
    }
}
