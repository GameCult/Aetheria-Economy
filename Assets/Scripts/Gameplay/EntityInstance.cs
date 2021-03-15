using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class EntityInstance : MonoBehaviour
{
    public Material InvisibleMaterial;
    public static Transform EffectManagerParent;
    public ShieldManager Shield;
    public HullCollider[] HullColliders;

    public RadiatorHardpoint[] RadiatorHardpoints;
    public ThrusterHardpoint[] ThrusterHardpoints;
    public WeaponHardpoint[] WeaponHardpoints;
    public ArticulationPoint[] ArticulationPoints;

    public GameObject DestroyEffect;
    
    public event Action OnFadedOut;
    public event Action OnFadedIn;
    
    private List<Material> _fadeMaterials = new List<Material>();
    private Dictionary<MeshRenderer, Material[]> _meshes = new Dictionary<MeshRenderer, Material[]>();
    private List<(MeshRenderer mesh, int submesh, Material material)> _nonFadedSubmeshes = new List<(MeshRenderer mesh, int submesh, Material material)>();
    private List<IDisposable> _subscriptions = new List<IDisposable>();
    private float _fade = 0;
    private bool _fading;
    private bool _fadingIn;
    private bool _fadedElementsVisible = false;
    private bool _unfadedElementsVisible = false;
    private float _fadeTime;
    
    private static Dictionary<InstantWeaponData, InstantWeaponEffectManager> _instantWeaponManagers = new Dictionary<InstantWeaponData, InstantWeaponEffectManager>();
    private static Dictionary<ConstantWeaponData, ConstantWeaponEffectManager> _constantWeaponManagers = new Dictionary<ConstantWeaponData, ConstantWeaponEffectManager>();

    public Dictionary<HardpointData, Transform[]> Barrels { get; private set; }
    public Dictionary<HardpointData, int> BarrelIndices { get; private set; }
    public Dictionary<Radiator, MeshRenderer> RadiatorMeshes { get; private set; }
    public Transform LookAtPoint { get; private set; }
    public Entity Entity { get; private set; }
    public ZoneRenderer ZoneRenderer { get; private set; }
    public bool Visible
    {
        get => _fade > 0.01f;
    }

    private void Awake()
    {
        var meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        var materials = new List<(Material material, List<(MeshRenderer renderer, int index)> submeshes)>();
        foreach (var mesh in meshes)
        {
            _meshes.Add(mesh, mesh.sharedMaterials);
            for (var i = 0; i < mesh.sharedMaterials.Length; i++)
            {
                var material = mesh.sharedMaterials[i];
                if (material.shader.FindPropertyIndex("_Fade") >= 0)
                {
                    var match = materials.FirstOrDefault(lm => lm.material == material);
                    if(match.material==null || material.shader.FindPropertyIndex("_EmissionFresnel") >= 0)
                    {
                        match = (material, new List<(MeshRenderer renderer, int index)>());
                        materials.Add(match);
                    }
                    match.submeshes.Add((mesh, i));
                }
                else
                {
                    _nonFadedSubmeshes.Add((mesh, i, material));
                    _meshes[mesh][i] = InvisibleMaterial;
                }
            }
        }

        foreach (var (material, submeshes) in materials)
        {
            var materialInstance = Instantiate(material);
            _fadeMaterials.Add(materialInstance);
            foreach (var (mesh, index) in submeshes)
            {
                _meshes[mesh][index] = materialInstance;
            }
            materialInstance.SetFloat("_Fade", 0);
        }

        foreach (var mesh in _meshes.Keys) mesh.enabled = false;

        foreach (var mesh in _meshes) mesh.Key.sharedMaterials = mesh.Value;
    }

    protected virtual void ShowUnfadedElements()
    {
        _unfadedElementsVisible = true;
        foreach (var (mesh, submesh, material) in _nonFadedSubmeshes) _meshes[mesh][submesh] = material;
        foreach (var mesh in _meshes) mesh.Key.sharedMaterials = mesh.Value;
    }

    protected virtual void HideUnfadedElements()
    {
        _unfadedElementsVisible = false;
        foreach (var (mesh, submesh, material) in _nonFadedSubmeshes) _meshes[mesh][submesh] = InvisibleMaterial;
        foreach (var mesh in _meshes) mesh.Key.sharedMaterials = mesh.Value;
    }

    public void FadeIn(float time)
    {
        if (_fade > .99f)
        {
            _fading = false;
            return;
        }
        _fadeTime = time;
        _fading = true;
        _fadingIn = true;
    }

    public void FadeOut(float time)
    {
        if (_fade < .01f)
        {
            _fading = false;
            return;
        }
        _fadeTime = time;
        _fading = true;
        _fadingIn = false;
    }

    public virtual void SetEntity(ZoneRenderer zoneRenderer, Entity entity)
    {
        gameObject.name = entity.Name;
        Entity = entity;
        ZoneRenderer = zoneRenderer;
        var hullData = entity.ItemManager.GetData(entity.Hull) as HullData;

        if(Shield)
            Shield.Entity = entity;
        foreach (var hullCollider in HullColliders) hullCollider.Entity = entity;

        foreach (var item in entity.Equipment)
        {
            foreach (var behavior in item.Behaviors)
            {
                if (behavior is InstantWeapon instantWeapon)
                {
                    var data = (InstantWeaponData) instantWeapon.Data;
                    if (!_instantWeaponManagers.ContainsKey(data))
                    {
                        var managerPrefab = UnityHelpers.LoadAsset<InstantWeaponEffectManager>(data.EffectPrefab);
                        if(managerPrefab)
                        {
                            _instantWeaponManagers.Add(data, Instantiate(managerPrefab, EffectManagerParent));
                        }
                        else Debug.LogError($"No InstantWeaponEffectManager prefab found at path {data.EffectPrefab}");
                    }

                    instantWeapon.OnFire += () => 
                        _instantWeaponManagers[data].Fire(instantWeapon, item, this, entity.Target.Value != null && ZoneRenderer.EntityInstances.ContainsKey(entity.Target.Value) ? ZoneRenderer.EntityInstances[entity.Target.Value] : null);

                    if (behavior is ChargedWeapon chargedWeapon)
                    {
                        var chargeManager = _instantWeaponManagers[data].GetComponent<ChargeEffectManager>();
                        if (chargeManager)
                        {
                            chargedWeapon.OnStartCharging += () => chargeManager.StartCharging(chargedWeapon, item, this);
                            chargedWeapon.OnStopCharging += () => chargeManager.StopCharging(chargedWeapon);
                            chargedWeapon.OnCharged += () => chargeManager.Charged(chargedWeapon);
                            chargedWeapon.OnFailed += () => chargeManager.Failed(chargedWeapon);
                        }
                    }
                }

                if (behavior is ConstantWeapon constantWeapon)
                {
                    var data = (ConstantWeaponData) constantWeapon.Data;
                    if (!_constantWeaponManagers.ContainsKey(data))
                    {
                        var managerPrefab = UnityHelpers.LoadAsset<ConstantWeaponEffectManager>(data.EffectPrefab);
                        if(managerPrefab)
                        {
                            _constantWeaponManagers.Add(data, Instantiate(managerPrefab, EffectManagerParent));
                        }
                        else Debug.LogError($"No ConstantWeaponEffectManager prefab found at path {data.EffectPrefab}");
                    }

                    constantWeapon.OnStartFiring += () =>
                        _constantWeaponManagers[data].StartFiring(data, item, this, entity.Target.Value != null ? ZoneRenderer.EntityInstances[entity.Target.Value] : null);
                    constantWeapon.OnStopFiring += () => 
                        _constantWeaponManagers[data].StopFiring(item);
                }
            }
        }
        RadiatorMeshes = new Dictionary<Radiator, MeshRenderer>();
        Barrels = new Dictionary<HardpointData, Transform[]>();
        BarrelIndices = new Dictionary<HardpointData, int>();
        foreach (var radiator in entity.GetBehaviors<Radiator>())
        {
            var hp = Entity.Hardpoints[radiator.Item.Position.x, radiator.Item.Position.y];
            if (hp != null && hp.Type == HardpointType.Radiator)
            {
                var mesh = RadiatorHardpoints.FirstOrDefault(x => x.name == hp.Transform);
                if (mesh)
                {
                    RadiatorMeshes.Add(radiator, mesh.Mesh);
                }
            }
        }
        foreach (var hp in hullData.Hardpoints)
        {
            if(hp.Type == HardpointType.Ballistic || hp.Type == HardpointType.Energy || hp.Type == HardpointType.Launcher)
            {
                var whp = WeaponHardpoints.FirstOrDefault(x => x.name == hp.Transform);
                if (whp)
                {
                    Barrels.Add(hp, whp.FiringPoint);
                    BarrelIndices.Add(hp, 0);
                }
            }
        }

        void DamageSchematic(float damage, Shape hitShape)
        {
            foreach (var v in hitShape.Coordinates)
                hitShape[v] = hitShape[v] && hullData.Shape[v];

            float hullDamage = 0;
            var damagePerCell = damage / hitShape.Coordinates.Length;
            foreach (var v in hitShape.Coordinates)
            {
                var d = damagePerCell;
                
                // Subtract surface damage from armor, passing on the remainder to the item and then to the hull
                var prev = entity.Armor[v.x, v.y];
                entity.Armor[v.x, v.y] = max(prev - d, 0);
                entity.ArmorDamage.OnNext((v, d));
                d = max(d - prev, 0);

                if (d > 0.1f)
                {
                    var item = entity.GearOccupancy[v.x, v.y];
                    if (item != null)
                    {
                        prev = item.EquippableItem.Durability;
                        item.EquippableItem.Durability = max(prev - d, 0);
                        entity.ItemDamage.OnNext((item, d));
                        d = max(d - prev, 0);
                    }
                }

                hullDamage += d;
            }

            if(hullDamage > .1f)
            {
                entity.Hull.Durability -= hullDamage;
                entity.HullDamage.OnNext(hullDamage);
            }
        }

        foreach (var collider in HullColliders)
        {
            collider.Splash.Subscribe(splash =>
            {
                var hitShape = new Shape(hullData.Shape.Width, hullData.Shape.Height);
                foreach (var v in hullData.Shape.Coordinates)
                {
                    var localHitDirection = transform.InverseTransformDirection(splash.Direction);
                    var direction = normalize(float2(localHitDirection.x, localHitDirection.z));
                    var cellDot = dot(normalize(v - hullData.Shape.CenterOfMass), direction);
                    if (cellDot < 0) hitShape[v] = true;
                }
                DamageSchematic(splash.Damage, hitShape);
            });
            
            collider.Hit.Subscribe(hit =>
            {
                var hardpointIndex = (int) hit.TexCoord.x - 1;
                
                var hitShape = new Shape(hullData.Shape.Width, hullData.Shape.Height);

                // U coordinate between 0-1 indicates a hit that didn't land directly on a hardpoint
                // Find the 2D position of the hit scaled to the schematic
                float2 hitPos = float2.zero;
                if (hardpointIndex < 0)
                {
                    hitPos = float2(hit.TexCoord.x * hullData.Shape.Width, hit.TexCoord.y * hullData.Shape.Height);
                    // Search all schematic border cells for the cell which is closest to the hit position
                    var hitCell = int2(-1);
                    var distance = float.MaxValue;
                    foreach (var v in hullData.Shape.Coordinates)
                    {
                        var cellDist = lengthsq(hitPos - v);
                        if (cellDist < distance)
                        {
                            distance = cellDist;
                            hitCell = v;
                        }
                    }

                    hitShape[hitCell] = true;
                }
                else
                {
                    // Collider UV coordinates starting with 1 correspond to hardpoint index
                    var hardpoint = hullData.Hardpoints[hardpointIndex];
                    
                    // Obtain the hull coordinates of all cells occupied by the hardpoint
                    var hardpointCells = hullData.Shape.Inset(hardpoint.Shape, hardpoint.Position);
                    hitPos = hardpointCells.CenterOfMass;
                    foreach (var v in hardpointCells.Coordinates)
                        hitShape[v] = true;
                }
                
                for (int i = 0; i < Mathf.RoundToInt(hit.Spread); i++)
                {
                    hitShape = hitShape.Expand();
                }

                if (hit.Penetration > .5f)
                {
                    // Find the local 2D vector corresponding to the direction of the incoming hit
                    var localHitDirection = transform.InverseTransformDirection(hit.Direction);
                    var penetrationVector = normalize(float2(localHitDirection.x, localHitDirection.z));

                    // March a ray through the ship from the hit position
                    var penetrationPoint = hitPos;
                    var penetrationDistance = 0;
                    while (penetrationDistance < hit.Penetration)
                    {
                        penetrationPoint += penetrationVector * .5f;
                        hitShape[int2(penetrationPoint)] = true;
                    }
                }
                
                DamageSchematic(hit.Damage, hitShape);
            });
        }

        LookAtPoint = new GameObject($"{entity.Name} Look Point").transform;
        
        foreach (var articulationPoint in ArticulationPoints)
        {
            articulationPoint.Target = LookAtPoint;
        }

        _subscriptions.Add(Entity.HullDamage.Subscribe(_ =>
        {
            if (Entity.Hull.Durability < .01f)
            {
                if (DestroyEffect != null)
                {
                    var t = Instantiate(DestroyEffect).transform;
                    t.position = transform.position;
                }
                entity.Zone.Entities.Remove(entity);
            }
        }));

        // _subscriptions.Add(entity.Docked.Take(1).Subscribe(parent =>
        // {
        //     if (Visible)
        //     {
        //         FadeOut(sectorRenderer.EntityFadeTime);
        //         OnFadedOut += () => entity.Zone.Entities.Remove(entity);
        //     }
        //     else entity.Zone.Entities.Remove(entity);
        // }));
    }
    
    public Transform GetBarrel(HardpointData hardpoint)
    {
        if (Barrels.ContainsKey(hardpoint))
        {
            var barrel = Barrels[hardpoint][BarrelIndices[hardpoint]];
            BarrelIndices[hardpoint] = (BarrelIndices[hardpoint] + 1) % Barrels[hardpoint].Length;
            return barrel;
        }

        return transform;
    }

    public virtual void Update()
    {
        if (_fading)
        {
            if (_fadingIn)
            {
                _fade += Time.deltaTime / _fadeTime;
                if (!_fadedElementsVisible && _fade > .01f)
                {
                    _fadedElementsVisible = true;
                    foreach (var mesh in _meshes.Keys) mesh.enabled = true;
                }
                if (_fade > 1)
                {
                    _fade = 1;
                    _fading = false;
                    OnFadedIn?.Invoke();
                    OnFadedIn = null;
                    ShowUnfadedElements();
                }
                foreach(var material in _fadeMaterials) material.SetFloat("_Fade", _fade);
            }
            else
            {
                _fade -= Time.deltaTime / _fadeTime;
                if(_unfadedElementsVisible && _fade < .99f) HideUnfadedElements();
                if (_fade < 0)
                {
                    _fadedElementsVisible = false;
                    foreach (var mesh in _meshes.Keys) mesh.enabled = false;
                    _fade = 0;
                    _fading = false;
                    OnFadedOut?.Invoke();
                    OnFadedOut = null;
                }
                foreach(var material in _fadeMaterials) material.SetFloat("_Fade", _fade);
            }
        }
        
        foreach (var x in RadiatorMeshes)
        {
            x.Value.material.SetFloat("_Emission", Entity.ItemManager.GameplaySettings.TemperatureEmissionCurve.Evaluate(x.Key.Temperature));
        }

        foreach (var x in Barrels)
        {
            Entity.HardpointTransforms[x.Key] = (x.Value[0].position, x.Value[0].forward);
        }

        LookAtPoint.position = transform.position + (Vector3) Entity.LookDirection * 
            (Entity.Target.Value != null ? max(Entity.TargetRange,Entity.ItemManager.GameplaySettings.ConvergenceMinimumDistance) : 10000);
        transform.position = Entity.Position;
    }

    private void OnDestroy()
    {
        foreach(var x in _subscriptions)
            x.Dispose();
    }
}
