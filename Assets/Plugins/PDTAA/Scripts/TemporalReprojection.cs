// Copyright (c) <2015> <Playdead>
// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE.TXT)
// AUTHOR: Lasse Jon Fuglsang Pedersen <lasse@playdead.com>

#if UNITY_5_5_OR_NEWER
#define SUPPORT_STEREO
#endif

using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera), typeof(FrustumJitter), typeof(VelocityBuffer))]
[AddComponentMenu("Playdead/TemporalReprojection")]
public class TemporalReprojection : EffectBase
{
    private static RenderBuffer[] mrt = new RenderBuffer[2];

    private Camera _camera;
    private FrustumJitter _frustumJitter;
    private VelocityBuffer _velocityBuffer;

    #region dithering
    public Texture ditherTex = null;

    private const int NUM_DITHEROFS = 1024;
    const float DITHERSIZ = 256;
    private static int frameofs = 0;
    private static Vector4[] ditheroffsets = null;
    public static Vector4 GetFrame_DitherOffset()
    {
        if ( ditheroffsets == null )
            init_ditheroffsets();

        frameofs = (frameofs+1) % NUM_DITHEROFS;
        return ditheroffsets[frameofs];
    }
    private static void init_ditheroffsets()
    {
        if ( ditheroffsets != null )
            return;

        ditheroffsets = new Vector4[NUM_DITHEROFS];

        for ( int i=0, n=ditheroffsets.Length; i<n; ++i )
        {
            Vector4 p = new Vector4( DITHERSIZ * FrustumJitter.HaltonSeq(2, i+1),
                                     DITHERSIZ * FrustumJitter.HaltonSeq(3, i+1),
                                     DITHERSIZ * FrustumJitter.HaltonSeq(5, i+1),
                                     DITHERSIZ * FrustumJitter.HaltonSeq(7, i+1) );

            ditheroffsets[i] = new Vector4( Mathf.Floor(p.x),
                                            Mathf.Floor(p.y),
                                            Mathf.Floor(p.z),
                                            Mathf.Floor(p.w) );
        }
    }
    #endregion

    public Shader reprojectionShader;
    private Material reprojectionMaterial;
    private RenderTexture[,] reprojectionBuffer;
    private int[] reprojectionIndex = new int[2] { -1, -1 };

    public enum Dilation
    {
        None, Dilate5X, Dilate3X3
    };
    public enum HistoryRectification
    {
        Clamping, CenterClipping, Clipping
    };
    public enum ColorSpace
    {
        RGB, Chroma /*YCoCg, YCrCg?*/
    };

    //public bool unjitterColorSamples = true;
    //public bool unjitterNeighborhood = false;
    //public bool unjitterReprojection = false;

    [Tooltip("TAA colorspace. RGB is faster, Chroma is more better")]
    public ColorSpace colorspace = ColorSpace.RGB;

    [Tooltip("use soft-cubic for sampling maincolor")]
    public bool useHigherOrderFilteringMainColor = false;

    [Tooltip("use catmull-rom for sampling historybuffer")]
    public bool useHigherOrderFilteringHistory = true;

    [Tooltip("History color restricted by clamping (faster), centerClipping or clipping (better)")]
    public HistoryRectification historyRectification = HistoryRectification.Clamping;

    [Tooltip("Shrink color-aabb using variance of neighborhood")]
    public bool useVarianceClipping = true;

    [Tooltip("Smooth color-aabb using rounding")]
    public bool useAABBRounding = true;

    public Dilation dilation = Dilation.Dilate3X3;
    public bool useMotionBlur = true;

    [Range(0.0f, 1.0f)] public float feedbackMin = 0.88f;
    [Range(0.0f, 1.0f)] public float feedbackMax = 0.97f;

    public float motionBlurStrength = 1.0f;
    public bool motionBlurIgnoreFF = false;

    #region shaderparms
    int jitter_id = -1; int JitterUVID() { if (jitter_id < 0) jitter_id = Shader.PropertyToID("_JitterUV"); return jitter_id; }
    int vb_id =-1; int VelocityBufferID() { if (vb_id<0) vb_id=Shader.PropertyToID( "_VelocityBuffer" ); return vb_id; }
    int vnm_id=-1; int VelocityNeighborMaxID() { if (vnm_id<0) vnm_id=Shader.PropertyToID( "_VelocityNeighborMax" ); return vnm_id; }
    int maintex_id=-1; int MainTexID() { if (maintex_id<0) maintex_id=Shader.PropertyToID( "_MainTex" ); return maintex_id; }
    int prevtex_id=-1; int PrevTexID() { if (prevtex_id<0) prevtex_id=Shader.PropertyToID( "_PrevTex" ); return prevtex_id; }
    int fbmin_id=-1; int FeedbackMinID() { if (fbmin_id<0) fbmin_id=Shader.PropertyToID( "_FeedbackMin" ); return fbmin_id; }
    int fbmax_id=-1; int FeedbackMinMaxID() { if (fbmax_id<0) fbmax_id=Shader.PropertyToID( "_FeedbackMinMax" ); return fbmax_id; }
    int ms_id=-1; int MotionScaleID() { if (ms_id<0) ms_id=Shader.PropertyToID( "_MotionScale" ); return ms_id; }
    int dithertex_id=-1; int DitherTexID() { if (dithertex_id<0) dithertex_id=Shader.PropertyToID( "_DitherTex" ); return dithertex_id; }
    int ditherofslocal_id=-1; int DitherOffsetLocalID() { if (ditherofslocal_id<0) ditherofslocal_id=Shader.PropertyToID( "_DitherOffset_local" ); return ditherofslocal_id; }
    #endregion

    void Reset()
    {
        _camera = GetComponent<Camera>();
        _frustumJitter = GetComponent<FrustumJitter>();
        _velocityBuffer = GetComponent<VelocityBuffer>();
    }

    void Clear()
    {
        EnsureArray(ref reprojectionIndex, 2);
        reprojectionIndex[0] = -1;
        reprojectionIndex[1] = -1;
    }

    void Awake()
    {
        Reset();
        Clear();
    }

    void Resolve(RenderTexture source, RenderTexture destination)
    {
        EnsureArray(ref reprojectionBuffer, 2, 2);
        EnsureArray(ref reprojectionIndex, 2, initialValue: -1);

        EnsureMaterial(ref reprojectionMaterial, reprojectionShader);
        if (reprojectionMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

#if SUPPORT_STEREO
        int eyeIndex = (_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right) ? 1 : 0;
#else
        int eyeIndex = 0;
#endif
        int bufferW = source.width;
        int bufferH = source.height;

        if (EnsureRenderTarget(ref reprojectionBuffer[eyeIndex, 0], bufferW, bufferH, RenderTextureFormat.ARGB32, FilterMode.Bilinear, antiAliasing: source.antiAliasing))
            Clear();
        if (EnsureRenderTarget(ref reprojectionBuffer[eyeIndex, 1], bufferW, bufferH, RenderTextureFormat.ARGB32, FilterMode.Bilinear, antiAliasing: source.antiAliasing))
            Clear();

#if SUPPORT_STEREO
        bool stereoEnabled = _camera.stereoEnabled;
#else
        bool stereoEnabled = false;
#endif
#if UNITY_EDITOR
        bool allowMotionBlur = !stereoEnabled && Application.isPlaying;
#else
        bool allowMotionBlur = !stereoEnabled;
#endif

        EnsureKeyword(reprojectionMaterial, "CAMERA_PERSPECTIVE", !_camera.orthographic);
        EnsureKeyword(reprojectionMaterial, "CAMERA_ORTHOGRAPHIC", _camera.orthographic);
        //EnsureKeyword(reprojectionMaterial, "UNJITTER_COLORSAMPLES", unjitterColorSamples);
        //EnsureKeyword(reprojectionMaterial, "UNJITTER_NEIGHBORHOOD", unjitterNeighborhood);
        //EnsureKeyword(reprojectionMaterial, "UNJITTER_REPROJECTION", unjitterReprojection);
        EnsureKeyword(reprojectionMaterial, "USE_CHROMA_COLORSPACE", colorspace == ColorSpace.Chroma);
        EnsureKeyword(reprojectionMaterial, "USE_CLIPPING", historyRectification == HistoryRectification.CenterClipping || historyRectification == HistoryRectification.Clipping );
        EnsureKeyword(reprojectionMaterial, "USE_CENTER_CLIPPING", historyRectification == HistoryRectification.CenterClipping );
        EnsureKeyword(reprojectionMaterial, "USE_VARIANCE_CLIPPING", useVarianceClipping );
        EnsureKeyword(reprojectionMaterial, "USE_AABB_ROUNDING", useAABBRounding );
        EnsureKeyword(reprojectionMaterial, "USE_DILATION_5X", dilation == Dilation.Dilate5X);
        EnsureKeyword(reprojectionMaterial, "USE_DILATION_3X3", dilation == Dilation.Dilate3X3);
        EnsureKeyword(reprojectionMaterial, "USE_MOTION_BLUR", useMotionBlur && allowMotionBlur);
        EnsureKeyword(reprojectionMaterial, "USE_MOTION_BLUR_NEIGHBORMAX", _velocityBuffer.activeVelocityNeighborMax != null);
        EnsureKeyword(reprojectionMaterial, "USE_HIGHER_ORDER_TEXTURE_FILTERING_COLOR", useHigherOrderFilteringMainColor);
        EnsureKeyword(reprojectionMaterial, "USE_HIGHER_ORDER_TEXTURE_FILTERING_HISTORY", useHigherOrderFilteringHistory);

        if (reprojectionIndex[eyeIndex] == -1)// bootstrap
        {
            reprojectionIndex[eyeIndex] = 0;
            reprojectionBuffer[eyeIndex, reprojectionIndex[eyeIndex]].DiscardContents();
            Graphics.Blit(source, reprojectionBuffer[eyeIndex, reprojectionIndex[eyeIndex]]);
        }

        int indexRead = reprojectionIndex[eyeIndex];
        int indexWrite = (reprojectionIndex[eyeIndex] + 1) % 2;

        Vector4 jitterUV = _frustumJitter.activeSample;
        jitterUV.x /= source.width;
        jitterUV.y /= source.height;
        jitterUV.z /= source.width;
        jitterUV.w /= source.height;

        reprojectionMaterial.SetVector(JitterUVID(), jitterUV);
        reprojectionMaterial.SetTexture(VelocityBufferID(), _velocityBuffer.activeVelocityBuffer);
        reprojectionMaterial.SetTexture(VelocityNeighborMaxID(), _velocityBuffer.activeVelocityNeighborMax);
        reprojectionMaterial.SetTexture(MainTexID(), source);
        reprojectionMaterial.SetTexture(PrevTexID(), reprojectionBuffer[eyeIndex, indexRead]);
        reprojectionMaterial.SetFloat(FeedbackMinID(), feedbackMin);
        reprojectionMaterial.SetFloat(FeedbackMinMaxID(), feedbackMax - feedbackMin);
        reprojectionMaterial.SetFloat(MotionScaleID(), motionBlurStrength * (motionBlurIgnoreFF ? Mathf.Min(1.0f, 1.0f / _velocityBuffer.timeScale) : 1.0f));

        reprojectionMaterial.SetTexture(DitherTexID(), ditherTex);
        Vector4 ofs = GetFrame_DitherOffset();
        reprojectionMaterial.SetVector( DitherOffsetLocalID(), GetFrame_DitherOffset() );

        // reproject frame n-1 into output + history buffer
        {
            mrt[0] = reprojectionBuffer[eyeIndex, indexWrite].colorBuffer;
            mrt[1] = destination.colorBuffer;

            Graphics.SetRenderTarget(mrt, source.depthBuffer);
            reprojectionMaterial.SetPass(0);
            reprojectionBuffer[eyeIndex, indexWrite].DiscardContents();

            //DrawFullscreenQuad();
            DrawFullscreenTri();

            reprojectionIndex[eyeIndex] = indexWrite;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (destination != null && source.antiAliasing == destination.antiAliasing)// resolve without additional blit when not end of chain
        {
            Resolve(source, destination);
        }
        else
        {
            RenderTexture internalDestination = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, source.antiAliasing);
            {
                Resolve(source, internalDestination);
                Graphics.Blit(internalDestination, destination);
            }
            RenderTexture.ReleaseTemporary(internalDestination);
        }
    }

    void OnApplicationQuit()
    {
        if (reprojectionBuffer != null)
        {
            ReleaseRenderTarget(ref reprojectionBuffer[0, 0]);
            ReleaseRenderTarget(ref reprojectionBuffer[0, 1]);
            ReleaseRenderTarget(ref reprojectionBuffer[1, 0]);
            ReleaseRenderTarget(ref reprojectionBuffer[1, 1]);
        }
    }
}