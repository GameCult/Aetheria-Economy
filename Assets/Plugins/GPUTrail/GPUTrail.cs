using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

public class GPUTrail : GPUTrailBase
{
    LinkedList<Vector3> _posLog = new LinkedList<Vector3>();
    int _totalInputIdx = -1;

    ComputeBuffer _inputBuffer;

    protected float _startTime;

    protected override int TrailNumMax { get { return 1; } }

    protected override void Awake()
    {
        base.Awake();

        _inputBuffer = new ComputeBuffer(InputNumMax, Marshal.SizeOf(typeof(Node)));
    }


    void Start()
    {
        _posLog.AddLast(transform.position);
    }

    protected override void ReleaseBuffer()
    {
        base.ReleaseBuffer();
        if (_inputBuffer != null) _inputBuffer.Release();
    }


    void _LerpPos(int inputNum, Vector3 pos)
    {
        var timeStep = Time.deltaTime / inputNum;
        var timePrev = Time.time - Time.deltaTime;

        var posPrev = _posLog.Last();
        var posStep = (pos - posPrev) / inputNum;

        for (var i = 1; i < inputNum; ++i)
        {
            _newPoints.Add(new Node()
            {
                pos = posPrev + posStep * i,
                time = timePrev + timeStep * i
            });
        }
    }

    List<Node> _newPoints = new List<Node>();
    protected override void UpdateVertex()
    {
        var pos = transform.position;
        var posPrev = _posLog.Last();

        if ((Vector3.Distance(posPrev, pos) > MINNodeDistance))
        {
            var inputNum = Mathf.Clamp(Mathf.FloorToInt(Time.deltaTime * InputPerSec), 1, InputNumMax);
            //inputNum = 1;

            if (inputNum > 1)
            {
                _LerpPos(inputNum, pos);
            }

            _newPoints.Add(new Node()
            {
                pos = pos,
                time = Time.time
            });

            _posLog.AddLast(pos);

            // _posLogには過去２つの位置を保存しとく
            for (var i = 0; i < _posLog.Count - 2; ++i)
            {
                _posLog.RemoveFirst();
            }
        }

        _UpdateVertex(_newPoints);

        _newPoints.Clear();
    }

    const int NUM_THREAD_X = 16;
    void _UpdateVertex(List<Node> newPoints)
    {
        Assert.IsTrue(newPoints.Count <= InputNumMax);

        var inputNum = newPoints.Count;
        if (inputNum > 0)
        {
            _inputBuffer.SetData(newPoints.ToArray());
            if (_totalInputIdx < 0) _startTime = Time.time;
            _totalInputIdx += inputNum;
        }

        if (_totalInputIdx >= 0)
        {
            SetCommonParameterForCs();

            Cs.SetInt("_InputNum", inputNum);
            Cs.SetInt("_TotalInputIdx", _totalInputIdx);
            Cs.SetInt("_BufferSize", nodeNumPerTrail);
            Cs.SetFloat("_StartTime", _startTime);

            var kernel = Cs.FindKernel("CreateWidth");
            Cs.SetBuffer(kernel, "_InputBuffer", _inputBuffer);
            Cs.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);
            Cs.SetBuffer(kernel, "_VertexBuffer", vertexBuffer);

            Cs.Dispatch(kernel, Mathf.CeilToInt((float)nodeBuffer.count / NUM_THREAD_X), 1, 1);
        }
    }


    public bool _debugDrawLogPoint;
    public bool _debugDrawVertexBuf;

    public void OnDrawGizmosSelected()
    {
        if (_debugDrawLogPoint)
        {
            Gizmos.color = Color.magenta;
            _posLog.ToList().ForEach(p =>
            {
                Gizmos.DrawWireSphere(p, MINNodeDistance);
            });
        }

        if (_debugDrawVertexBuf)
        {
            Gizmos.color = Color.yellow;
            var data = new Vertex[vertexBuffer.count];
            vertexBuffer.GetData(data);

            var num = vertexBuffer.count / 2;
            for(var i=0; i< num; ++i)
            {
                var v0 = data[2*i];
                var v1 = data[2*i +1];

                Gizmos.DrawLine(v0.pos, v1.pos);
			}
        }
    }
}
