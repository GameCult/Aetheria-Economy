using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

public abstract class GPUTrailIndirect : GPUTrailBase
{
    #region TypeDefine
    public struct Trail
    {
        public float startTime;
        public int totalInputNum;
    }


    public struct InputData
    {
        public Vector3 position;
        public Color color;
    }
    #endregion

    protected ComputeBuffer _inputBuffer;
    ComputeBuffer _trailBuffer;


    override protected void Awake()
    {
        base.Awake();

        _trailBuffer = new ComputeBuffer(TrailNumMax, Marshal.SizeOf(typeof(Trail)));
        _trailBuffer.SetData(Enumerable.Repeat(default(Trail), TrailNumMax).ToArray());

        _inputBuffer = new ComputeBuffer(TrailNumMax, Marshal.SizeOf(typeof(InputData)));
    }

    override protected void ReleaseBuffer()
    {
        base.ReleaseBuffer();
        if (_inputBuffer != null) _inputBuffer.Release();
        if (_trailBuffer != null) _trailBuffer.Release();
    }

    const int NUM_THREAD_X = 32;
    protected override void UpdateVertex()
    {
        // AddNode
        SetCommonParameterForCs();

        var success = UpdateInputBuffer();
        if (success)
        {
            var kernel = Cs.FindKernel("AddNode");
            Cs.SetBuffer(kernel, "_InputBuffer", _inputBuffer);
            Cs.SetBuffer(kernel, "_TrailBufferW", _trailBuffer);
            Cs.SetBuffer(kernel, "_NodeBufferW", nodeBuffer);

            Cs.Dispatch(kernel, Mathf.CeilToInt((float)_trailBuffer.count / NUM_THREAD_X), 1, 1);

            // CreateWidth
            kernel = Cs.FindKernel("CreateWidth");
            Cs.SetBuffer(kernel, "_TrailBuffer", _trailBuffer);
            Cs.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);
            Cs.SetBuffer(kernel, "_VertexBuffer", vertexBuffer);
            Cs.Dispatch(kernel, Mathf.CeilToInt((float)nodeBuffer.count / NUM_THREAD_X), 1, 1);
        }
    }

    protected abstract bool UpdateInputBuffer();
}
