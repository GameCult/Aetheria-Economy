using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class SchematicDisplay : MonoBehaviour
{
    public GameSettings Settings;
    public Prototype ListElementPrototype;
    public Prototype TriggerGroupPrototype;

    public TextMeshProUGUI EnergyLabel;
    public TextMeshProUGUI CockpitTemperatureLabel;
    public TextMeshProUGUI RadiatorTemperatureLabel;
    public TextMeshProUGUI HeatsinkTemperatureLabel;
    public TextMeshProUGUI CargoTemperatureLabel;
    public TextMeshProUGUI VisibilityLabel;
    public TextMeshProUGUI HullDurabilityLabel;

    public RectTransform EnergyFill;
    public RectTransform HullDurabilityFill;
    public RectTransform HeatstrokeMeterFill;
    public RectTransform HeatstrokeLimitFill;

    public CanvasGroup TriggerGroups;
    public float TriggerGroupsFadeDuration = .5f;
    public float TriggerGroupsPersistDuration = 5f;

    public Color TriggerGroupColor;

    private Ship _ship;
    private EquippableItem _hull;
    private Radiator[] _radiators;
    private Cockpit _cockpit;
    private Reactor _reactor;
    private Capacitor[] _capacitors;
    private Heatsink[] _heatsinks;
    private EquippedCargoBay[] _cargoBays;
    private SchematicDisplayItem[] _schematicItems;
    private Graphic[] _groupGraphics;

    private int _selectedGroupIndex;
    private int _selectedItemIndex;
    private bool _triggerGroupsVisible;
    private float _triggerGroupsFadeOutTime;

    public SchematicDisplayItem[] SchematicItems
    {
        get { return _schematicItems; }
    }

    public int SelectedItemIndex
    {
        get => _selectedItemIndex;
        set
        {
            _selectedItemIndex = (value + _schematicItems.Length) % _schematicItems.Length;
            UpdateTriggerGroups();
        }
    }

    public int SelectedGroupIndex
    {
        get => _selectedGroupIndex;
        set
        {
            _selectedGroupIndex = (value + Settings.GameplaySettings.TriggerGroupCount) % Settings.GameplaySettings.TriggerGroupCount;
            UpdateTriggerGroups();
        }
    }

    public class SchematicDisplayItem
    {
        public EquippedItem Item;
        public SchematicListElement ListElement;
        public SchematicTriggerGroupElement TriggerGroupElement;
        public IProgressBehavior Cooldown;
        public ItemUsage ItemUsage;
        public WeaponData WeaponData;
    }

    public void ShowShip(Ship ship)
    {
        if (_schematicItems != null)
            foreach (var item in _schematicItems)
            {
                item.ListElement.GetComponent<Prototype>().ReturnToPool();
                item.TriggerGroupElement?.GetComponent<Prototype>().ReturnToPool();
            }
        _ship = ship;
        _cockpit = ship.GetBehavior<Cockpit>();
        _reactor = ship.GetBehavior<Reactor>();
        _capacitors = ship.GetBehaviors<Capacitor>().ToArray();
        
        _radiators = ship.GetBehaviors<Radiator>().ToArray();
        if (_radiators.Length == 0)
            RadiatorTemperatureLabel.text = "N/A";
        
        _heatsinks = ship.GetBehaviors<Heatsink>().ToArray();
        if (_heatsinks.Length == 0)
            HeatsinkTemperatureLabel.text = "N/A";
        
        _cargoBays = ship.CargoBays.ToArray();
        if (_cargoBays.Length == 0)
            CargoTemperatureLabel.text = "N/A";
        
        _schematicItems = ship.Equipment
            .Where(x => x.Behaviors.Any(b => b.Data is WeaponData))
            .Select(x => new SchematicDisplayItem
            {
                Item = x, 
                ListElement = ListElementPrototype.Instantiate<SchematicListElement>(),
                TriggerGroupElement = TriggerGroupPrototype.Instantiate<SchematicTriggerGroupElement>(),
                Cooldown = (IProgressBehavior) x.Behaviors.FirstOrDefault(b=> b is IProgressBehavior),
                ItemUsage = (ItemUsage) x.Behaviors.FirstOrDefault(b=> b is ItemUsage),
                WeaponData = (WeaponData) x.Behaviors.FirstOrDefault(b=>b.Data is WeaponData)?.Data
            })
            .ToArray();
        foreach (var x in _schematicItems)
        {
            x.ListElement.Icon.sprite = Settings.ItemIcons[(int) ship.Hardpoints[x.Item.Position.x, x.Item.Position.y].Type];
            x.ListElement.Label.text = x.Item.EquippableItem.Name;
            x.ListElement.InfiniteAmmoIcon.gameObject.SetActive(x.ItemUsage == null);
            x.ListElement.AmmoLabel.gameObject.SetActive(x.ItemUsage != null);
            foreach (var group in x.TriggerGroupElement.GroupBackgrounds)
                group.color = TriggerGroupColor;
        }
        UpdateTriggerGroups();
    }

    public void UpdateTriggerGroups()
    {
        if (!_triggerGroupsVisible)
        {
            StartCoroutine(FadeInTriggerGroups());
            _triggerGroupsVisible = true;
        }
        _triggerGroupsFadeOutTime = Time.time + TriggerGroupsPersistDuration;
        for (var itemIndex = 0; itemIndex < _schematicItems.Length; itemIndex++)
        {
            var item = _schematicItems[itemIndex];
            for (var groupIndex = 0; groupIndex < item.TriggerGroupElement.GroupBackgrounds.Length; groupIndex++)
            {
                var backgroundColor = TriggerGroupColor;
                if (_selectedGroupIndex == groupIndex)
                    backgroundColor.a *= 2;
                if (_selectedItemIndex == itemIndex)
                    backgroundColor.a *= 2;
                item.TriggerGroupElement.GroupBackgrounds[groupIndex].color = backgroundColor;
                item.TriggerGroupElement.GroupLabel[groupIndex].color =
                    _ship.TriggerGroups[groupIndex].Contains(item.Item) ? Color.white : Color.gray;
            }
        }
    }

    IEnumerator FadeInTriggerGroups()
    {
        var startTime = Time.time;
        while (Time.time - startTime < TriggerGroupsFadeDuration)
        {
            var lerp = (Time.time - startTime) / TriggerGroupsFadeDuration;
            TriggerGroups.alpha = lerp;
            yield return null;
        }

        TriggerGroups.alpha = 1;
    }

    IEnumerator FadeOutTriggerGroups()
    {
        var startTime = Time.time;
        while (Time.time - startTime < TriggerGroupsFadeDuration)
        {
            var lerp = (Time.time - startTime) / TriggerGroupsFadeDuration;
            TriggerGroups.alpha = 1-lerp;
            yield return null;
        }

        TriggerGroups.alpha = 0;
    }
    
    void Update()
    {
        if (_triggerGroupsVisible && _triggerGroupsFadeOutTime < Time.time)
        {
            _triggerGroupsVisible = false;
            StartCoroutine(FadeOutTriggerGroups());
        }
        if (_ship != null)
        {
            VisibilityLabel.text = ((int) _ship.Visibility).ToString();
            
            if(_radiators.Length == 1)
                RadiatorTemperatureLabel.text = $"{(_radiators[0].Item.Temperature - 273.15f).SignificantDigits(3)}°C";
            else if(_radiators.Length > 1)
                RadiatorTemperatureLabel.text = 
                    $"{(_radiators.Min(r => r.Item.Temperature) - 273.15f).SignificantDigits(3)}-" +
                    $"{(_radiators.Max(r => r.Item.Temperature) - 273.15f).SignificantDigits(3)}°C";
            
            if(_heatsinks.Length == 1)
                HeatsinkTemperatureLabel.text = $"{(_heatsinks[0].Item.Temperature - 273.15f).SignificantDigits(3)}°C";
            else if(_heatsinks.Length > 1)
                HeatsinkTemperatureLabel.text = 
                    $"{(_heatsinks.Min(r => r.Item.Temperature) - 273.15f).SignificantDigits(3)}-" +
                    $"{(_heatsinks.Max(r => r.Item.Temperature) - 273.15f).SignificantDigits(3)}°C";
            
            if(_cargoBays.Length == 1)
                CargoTemperatureLabel.text = $"{(_cargoBays[0].Temperature - 273.15f).SignificantDigits(3)}°C";
            else if(_cargoBays.Length > 1)
                CargoTemperatureLabel.text = 
                    $"{(_cargoBays.Min(r => r.Temperature) - 273.15f).SignificantDigits(3)}-" +
                    $"{(_cargoBays.Max(r => r.Temperature) - 273.15f).SignificantDigits(3)}°C";
            
            CockpitTemperatureLabel.text = $"{(_cockpit.Item.Temperature - 273.15f).SignificantDigits(3)}°C";
            
            HeatstrokeMeterFill.anchorMax = new Vector2(_cockpit.Heatstroke, 1);
            HeatstrokeLimitFill.anchorMax = new Vector2(_cockpit.Item.Temperature / Settings.GameplaySettings.HeatstrokeTemperature, 1);

            _hull = _ship.Hull;
            var hullData = _ship.ItemManager.GetData(_hull);
            var dur = _hull.Durability / hullData.Durability;
            HullDurabilityFill.anchorMax = new Vector2(dur, 1);
            HullDurabilityLabel.text = $"{(dur*100).SignificantDigits(3)}%";
            
            if (_capacitors.Length == 0)
            {
                EnergyFill.anchorMax = Vector2.up;
                EnergyLabel.text = _reactor.Surplus.SignificantDigits(3);
            }
            else
            {
                var charge = _capacitors.Sum(x => x.Charge);
                var maxCharge = _capacitors.Sum(x => x.Capacity);
                EnergyFill.anchorMax = new Vector2(charge / maxCharge, 1);
                EnergyLabel.text = $"{charge.SignificantDigits(3)}/{maxCharge.SignificantDigits(3)} ({_reactor.Surplus.SignificantDigits(3)})";
            }

            foreach (var x in _schematicItems)
            {
                var itemData = _ship.ItemManager.GetData(x.Item.EquippableItem);
                x.ListElement.HeatFill.anchorMax = new Vector2(unlerp(itemData.MinimumTemperature,itemData.MaximumTemperature, x.Item.Temperature), 0);
                if(x.Cooldown!=null)
                    x.ListElement.CooldownFill.anchorMax = new Vector2(x.Cooldown.Progress,1);
                x.ListElement.DurabilityLabel.text = $"{(int)(x.Item.EquippableItem.Durability / itemData.Durability * 100)}%";
                if (x.ItemUsage != null)
                {
                    x.ListElement.AmmoLabel.text = _ship.CountItemsInCargo(((ItemUsageData) x.ItemUsage.Data).Item).ToString();
                }

                if (x.WeaponData != null)
                {
                    x.ListElement.RangeLabel.text = ((int) _ship.ItemManager.Evaluate(x.WeaponData.Range, x.Item.EquippableItem, _ship)).ToString();
                }
            }
        }
    }
}
