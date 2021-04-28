using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

public abstract class GPUTrailBase : MonoBehaviour
{
    #region TypeDefine
    
    public struct Node
    {
        public Vector3 pos;
        public float time;
        public Color color;
    }

    public struct Vertex
    {
        public Vector3 pos;
        public Vector2 uv;
        public Color color;
    }

    #endregion


    public ComputeShader Cs;
    public Material MaterialShared;
    public float Life = 10f;
    public float InputPerSec = 60f;
    public int InputNumMax = 5;
    public float MINNodeDistance = 0.1f;
    public float StartWidth = 1f;
    public float EndWidth = 1f;
    public bool Draw = true;

    protected int nodeNumPerTrail;

    protected ComputeBuffer nodeBuffer;
    protected ComputeBuffer vertexBuffer;
    protected ComputeBuffer indexBuffer;

    protected Material material;

    protected abstract int TrailNumMax { get; }
    public int NodeBufferSize { get { return TrailNumMax * nodeNumPerTrail; } }
    public int VertexBufferSize { get { return NodeBufferSize * 2; } }

    public int VertexNumPerTrail { get { return nodeNumPerTrail * 2; } }
    public int IndexNumPerTrail { get { return (nodeNumPerTrail - 1) * 6; } }

    protected virtual void Awake()
    {
        ReleaseBuffer();

        nodeNumPerTrail = Mathf.CeilToInt(Life * InputPerSec);
        InitBuffer();

        material = new Material(MaterialShared);
    }


    protected virtual void InitBuffer()
    {
        nodeBuffer = new ComputeBuffer(NodeBufferSize, Marshal.SizeOf(typeof(Node)));
        nodeBuffer.SetData(Enumerable.Repeat(default(Node), nodeBuffer.count).ToArray());

        vertexBuffer = new ComputeBuffer(VertexBufferSize, Marshal.SizeOf(typeof(Vertex))); // 1 node to 2 vtx(left,right)
        vertexBuffer.SetData(Enumerable.Repeat(default(Vertex), vertexBuffer.count).ToArray());

        // 各Nodeの最後と次のNodeの最初はポリゴンを繋がないので-1
        var indexData = new int[IndexNumPerTrail];
        var iidx = 0;
        for (var iNode = 0; iNode < nodeNumPerTrail - 1; ++iNode)
        {
            var offset = +iNode * 2;
            indexData[iidx++] = 0 + offset;
            indexData[iidx++] = 1 + offset;
            indexData[iidx++] = 2 + offset;
            indexData[iidx++] = 2 + offset;
            indexData[iidx++] = 1 + offset;
            indexData[iidx++] = 3 + offset;
        }

        indexBuffer = new ComputeBuffer(indexData.Length, Marshal.SizeOf(typeof(uint))); // 1 node to 2 triangles(6vertexs)
        indexBuffer.SetData(indexData);
    }



    protected virtual void ReleaseBuffer()
    {
        new[] { nodeBuffer, vertexBuffer, indexBuffer }
            .Where(b => b != null)
            .ToList().ForEach(buffer =>
            {
                buffer.Release();
            });
    }


    protected virtual bool IsCameraOrthographic { get { return Camera.main.orthographic; } }
    protected virtual Vector3 ToOrthographicCameraDir { get { return -Camera.main.transform.forward; } }
    protected virtual Vector3 CameraPos { get { return Camera.main.transform.position; } }

    protected void SetCommonParameterForCs()
    {
        _SetCommonParameterForCS(Cs);
    }
    protected void _SetCommonParameterForCS(ComputeShader cs)
    {
        cs.SetInt("_TrailNum", TrailNumMax);
        cs.SetInt("_NodeNumPerTrail", nodeNumPerTrail);

        cs.SetInt("_InputNodeNum", Mathf.Min(InputNumMax, Mathf.FloorToInt(_inputNumCurrent)));
        //cs.SetInt("_LerpType", (int)_lerpType);
        cs.SetFloat("_MinNodeDistance", MINNodeDistance);
        cs.SetFloat("_Time", Time.time);
        cs.SetFloat("_Life", Life);

        cs.SetVector("_ToCameraDir", IsCameraOrthographic ? ToOrthographicCameraDir : Vector3.zero);
        cs.SetVector("_CameraPos", CameraPos);
        cs.SetFloat("_StartWidth", StartWidth);
        cs.SetFloat("_EndWidth", EndWidth);
    }

    float _inputNumCurrent;
    protected virtual void LateUpdate()
    {
        _inputNumCurrent = Time.deltaTime * InputPerSec + (_inputNumCurrent - Mathf.Floor(_inputNumCurrent)); // continue under dicimal
        UpdateVertex();
    }

    protected abstract void UpdateVertex();


    void OnRenderObject()
    {
        if (Draw)
        {
            if ((Camera.current.cullingMask & (1 << gameObject.layer)) == 0)
            {
                return;
            }

            OnRenderObjectInternal();
        }
    }


    protected virtual void SetMaterialParam() { }
    protected virtual void SetCommonMaterialParam()
    {
        SetMaterialParam();
        material.SetInt("_VertexNumPerTrail", VertexNumPerTrail);
        material.SetBuffer("_IndexBuffer", indexBuffer);
        material.SetBuffer("_VertexBuffer", vertexBuffer);
    }

    protected virtual void OnRenderObjectInternal()
    {
        SetCommonMaterialParam();

        material.DisableKeyword("GPUTRAIL_TRAIL_INDEX_ON");
        material.SetPass(0);

        Graphics.DrawProceduralNow(MeshTopology.Triangles, indexBuffer.count, TrailNumMax);
    }

    public void OnDestroy()
    {
        ReleaseBuffer();

        Destroy(material);
    }
}
