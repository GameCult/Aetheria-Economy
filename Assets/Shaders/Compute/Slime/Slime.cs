using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MessagePack;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using static Unity.Mathematics.math;

public class Slime : MonoBehaviour
{
    #region variables
    
    [Header("Scene Links")]
    public Camera TargetCamera;
    
    [Header("Materials")]
    public ComputeShader SlimeCompute;
    public Material AccumulationMaterial;
    public Material ParticleMaterial;
    public RenderTexture GravityTexture;
    
    [Header("Performance")]
    public int TextureSize = 1024;
    public float ZoneRadius = 512;
    public int AgentCount = 524288;
    public int ParticleCount = 65536;
    public int TrailPoints = 8;

    [Header("Parameters")]
    public SlimeSettings SlimeSettings;
    public float ZoneCeiling;
    public float ZoneFloor;
    public float NoiseFrequency = 1;
    public float NoiseAmplitude = 5;
    public float ParticleHeightRange;
    public float ParticleHeightOffset;
    public float TrailStartWidth = .5f;
    public float TrailEndWidth = .1f;
    public float TrailDamping = .05f;

    private const int GROUP_SIZE = 64;
    private int _updateAgentsKernel;
    private int _diffuseKernel;
    private int _particleKernel;
    private int _geometryKernel;

    private int IndexBufferSize => ParticleCount * 6 * (TrailPoints - 1);
    
    #endregion

    #region buffers
    
    private ComputeBuffer _agentsBuffer;
    private RenderTexture _accumulationTexture;
    private RenderTexture _previousAccumulationTexture;
    
    private ComputeBuffer _particlesBuffer;

    private ComputeBuffer _indexBuffer;
    private ComputeBuffer _vertexBuffer;
    
    #endregion

    #region Structs
    
    struct Agent
    {
        public Vector2 Position;
        public float Angle;
    }
    
    struct Particle
    {
        public Vector3 Start;
        public Vector3 Middle;
        public Vector3 End;
    }
    
    struct Vertex
    {
        public Vector3 Position;
        public Vector2 UV;
    }
    
    #endregion
    
    void Start()
    {
        _updateAgentsKernel = SlimeCompute.FindKernel("UpdateAgents");
        _diffuseKernel = SlimeCompute.FindKernel("Diffuse");
        _particleKernel = SlimeCompute.FindKernel("CreateAgentParticles");
        _geometryKernel = SlimeCompute.FindKernel("CreateParticleGeometry");

        _accumulationTexture = new RenderTexture(TextureSize, TextureSize, 1, RenderTextureFormat.RFloat);
        _accumulationTexture.enableRandomWrite = true;
        _previousAccumulationTexture = new RenderTexture(TextureSize, TextureSize, 1, RenderTextureFormat.RFloat);
        _previousAccumulationTexture.enableRandomWrite = true;
        AccumulationMaterial.SetTexture("_MainTex", _accumulationTexture);

        _agentsBuffer = new ComputeBuffer(AgentCount, Marshal.SizeOf(typeof(Agent)));
        var agents = new Agent[AgentCount];
        for (var i = 0; i < agents.Length; i++)
        {
            agents[i] = new Agent
            {
                Position = Random.insideUnitCircle * ZoneRadius,
                Angle = Random.value * Mathf.PI * 2
            };
        }
        _agentsBuffer.SetData(agents);
        
        _particlesBuffer = new ComputeBuffer(ParticleCount, Marshal.SizeOf(typeof(Particle)));

        _vertexBuffer = new ComputeBuffer(ParticleCount * 2 * TrailPoints, Marshal.SizeOf(typeof(Vertex)));
        
        var indexData = new int[IndexBufferSize];
        var index = 0;
        for (int iTrail = 0; iTrail < ParticleCount; iTrail++)
        {
            var trailOffset = iTrail * TrailPoints * 2;
            for (int iTrailPoint = 0; iTrailPoint < TrailPoints - 1; iTrailPoint++)
            {
                var offset = trailOffset + iTrailPoint * 2;
                indexData[index++] = 0 + offset;
                indexData[index++] = 1 + offset;
                indexData[index++] = 2 + offset;
                indexData[index++] = 2 + offset;
                indexData[index++] = 1 + offset;
                indexData[index++] = 3 + offset;
            }
        }

        _indexBuffer = new ComputeBuffer(indexData.Length, Marshal.SizeOf(typeof(uint))); // 1 trail point to 2 triangles (6 vertices)
        _indexBuffer.SetData(indexData);
    }

    // Update is called once per frame
    void Update()
    {
        SlimeCompute.SetInt("randomOffset", Random.Range(0,65536));
        SlimeCompute.SetInt("textureWidth", TextureSize);
        SlimeCompute.SetInt("numAgents", AgentCount);
        SlimeCompute.SetFloat("zoneRadius", ZoneRadius);
        SlimeCompute.SetFloat("deposition", SlimeSettings.Deposition * Time.deltaTime);
        SlimeCompute.SetFloat("diffusion", SlimeSettings.Diffusion * Time.deltaTime);
        SlimeCompute.SetFloat("decay", max(1 - SlimeSettings.Decay * Time.deltaTime, 0.01f));
        SlimeCompute.SetFloat("speed", SlimeSettings.Speed * Time.deltaTime);
        SlimeCompute.SetFloat("turnSpeed", SlimeSettings.TurnSpeed * Time.deltaTime);
        SlimeCompute.SetFloat("randomSteering", SlimeSettings.RandomSteering);
        SlimeCompute.SetFloat("sensorDistance", SlimeSettings.SensorDistance);
        SlimeCompute.SetFloat("drive", SlimeSettings.Drive);
        SlimeCompute.SetFloat("sensorSpread", SlimeSettings.SensorSpread * Mathf.PI / 2);
        SlimeCompute.SetInt("sensorSize", SlimeSettings.SensorSize);

        SlimeCompute.SetBuffer(_updateAgentsKernel, "agents", _agentsBuffer);
        SlimeCompute.SetTexture(_updateAgentsKernel, "accumulation", _accumulationTexture);
        SlimeCompute.SetTexture(_updateAgentsKernel, "Heightmap", GravityTexture);
        
        var numberOfGroups = Mathf.CeilToInt((float)AgentCount / GROUP_SIZE);
        SlimeCompute.Dispatch(_updateAgentsKernel, numberOfGroups, 1, 1);

        Graphics.Blit(_accumulationTexture, _previousAccumulationTexture);
        SlimeCompute.SetTexture(_diffuseKernel, "accumulation", _previousAccumulationTexture);
        SlimeCompute.SetTexture(_diffuseKernel, "diffusedAccumulation", _accumulationTexture);
        numberOfGroups = Mathf.CeilToInt((float)TextureSize / 8);
        SlimeCompute.Dispatch(_diffuseKernel, numberOfGroups, numberOfGroups, 1);
        
        SlimeCompute.SetTexture(_particleKernel, "Heightmap", GravityTexture);
        SlimeCompute.SetBuffer(_particleKernel, "agents", _agentsBuffer);
        SlimeCompute.SetBuffer(_particleKernel, "particles", _particlesBuffer);
        SlimeCompute.SetFloat("time", Time.time * NoiseFrequency);
        SlimeCompute.SetFloat("noiseAmplitude", NoiseAmplitude);
        SlimeCompute.SetFloat("heightRange", ParticleHeightRange);
        SlimeCompute.SetFloat("heightOffset", ParticleHeightOffset);
        SlimeCompute.SetFloat("trailDamping", TrailDamping);
        SlimeCompute.SetInt("particleCount", ParticleCount);

        numberOfGroups = ParticleCount / GROUP_SIZE;
        SlimeCompute.Dispatch(_particleKernel, numberOfGroups, 1, 1);
        
        SlimeCompute.SetInt("trailPoints", TrailPoints);
        SlimeCompute.SetFloat("trailStartWidth", TrailStartWidth);
        SlimeCompute.SetFloat("trailEndWidth", TrailEndWidth);
        SlimeCompute.SetVector("cameraPos", TargetCamera.transform.position);
        SlimeCompute.SetBuffer(_geometryKernel, "particles", _particlesBuffer);
        SlimeCompute.SetBuffer(_geometryKernel, "vertexBuffer", _vertexBuffer);

        numberOfGroups = ParticleCount * TrailPoints / GROUP_SIZE;
        SlimeCompute.Dispatch(_geometryKernel, numberOfGroups, 1, 1);
        
        ParticleMaterial.SetBuffer("_IndexBuffer", _indexBuffer);
        ParticleMaterial.SetBuffer("_VertexBuffer", _vertexBuffer);
        Graphics.DrawProcedural(
            ParticleMaterial, 
            new Bounds(Vector3.zero, ZoneRadius * Vector3.one), 
            MeshTopology.Triangles, 
            IndexBufferSize, 
            1, 
            TargetCamera, 
            null, 
            ShadowCastingMode.Off, 
            false, 
            0);
    }

    private void OnDisable()
    {
        _accumulationTexture.Release();
        _agentsBuffer.Release();
    }
}

[Serializable, MessagePackObject(keyAsPropertyName:true)]
public class SlimeSettings
{
    public float Deposition;
    public float Diffusion;
    public float Decay;
    public float TurnSpeed;
    public float RandomSteering;
    public float Speed;
    public float Drive;
    public float SensorDistance;
    public float SensorSpread;
    public int SensorSize;
}
