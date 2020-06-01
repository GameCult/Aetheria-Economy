using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ColonyTab : MonoBehaviour
{
    public PropertiesPanel General;
    public PropertiesPanel Inventory;
    public PropertiesPanel Details;
    
    [HideInInspector]
    public GameContext Context;

    private Guid _selectedColony;

    public void Open(Guid colony)
    {
        _selectedColony = colony;
        
        General.Clear();
        Inventory.Clear();
        Details.Clear();
        
        var entity = Context.Cache.Get<Entity>(_selectedColony);

        General.AddProperty("Capacity", () => $"{entity.OccupiedCapacity}/{entity.Capacity:0}");
        General.AddProperty("Mass", () => $"{entity.Mass.SignificantDigits(Context.GlobalData.SignificantDigits)}");
        General.AddProperty("Temperature", () => $"{entity.Temperature:0}°K");
        General.AddProperty("Energy", () => $"{entity.Energy:0}/{entity.GetBehaviors<Reactor>().First().Capacitance:0}");
        General.AddProperty("Population", () => $"{entity.Population}");
        General.AddSection("Personality");
        foreach (var attribute in entity.Personality)
            General.AddPersonalityProperty(Context.Cache.Get<PersonalityAttribute>(attribute.Key),
                () => attribute.Value);
        General.RefreshValues();

        Inventory.ChildRadioSelection = true;
        Inventory.AddSection("Gear");
        var equippedItems = entity.EquippedItems.Select(g => Context.Cache.Get<Gear>(g));
        foreach(var gear in equippedItems)
            Inventory.AddProperty(gear.ItemData.Name, null, () => PopulateDetails(gear, entity));
        if(!entity.Cargo.Any())
            Inventory.AddSection("No Cargo");
        else
        {
            Inventory.AddSection("Cargo");
            foreach (var itemID in entity.Cargo)
            {
                var itemInstance = Context.Cache.Get<ItemInstance>(itemID);
                var data = Context.Cache.Get<ItemData>(itemInstance.Data);
                if(itemInstance is SimpleCommodity simpleCommodity)
                    Inventory.AddProperty(data.Name, () => simpleCommodity.Quantity.ToString());
                else
                    Inventory.AddProperty(data.Name);
            }
        }
        Inventory.RefreshValues();
    }
    
    void PopulateDetails(ItemInstance item, Entity entity)
    {
        Details.Clear();
        if (item is Gear gear)
        {
            var data = gear.ItemData;
            Details.AddProperty("Durability",
                () =>
                    $"{gear.Durability.SignificantDigits(Context.GlobalData.SignificantDigits)}/{Context.Evaluate(data.Durability, gear).SignificantDigits(Context.GlobalData.SignificantDigits)}");
            foreach (var behavior in data.Behaviors)
            {
                var type = behavior.GetType();
                if (type.GetCustomAttribute(typeof(RuntimeInspectable)) != null)
                {
                    foreach (var field in type.GetFields().Where(f => f.GetCustomAttribute<RuntimeInspectable>() != null))
                    {
                        var fieldType = field.FieldType;
                        if (fieldType == typeof(float))
                            Details.AddProperty(field.Name, () => $"{((float) field.GetValue(behavior)).SignificantDigits(Context.GlobalData.SignificantDigits)}");
                        else if (fieldType == typeof(int))
                            Details.AddProperty(field.Name, () => $"{(int) field.GetValue(behavior)}");
                        else if (fieldType == typeof(PerformanceStat))
                        {
                            var stat = (PerformanceStat) field.GetValue(behavior);
                            Details.AddProperty(field.Name, () => $"{Context.Evaluate(stat, gear, entity).SignificantDigits(Context.GlobalData.SignificantDigits)}");
                        }
                    }
                }
            }
        }
        Details.RefreshValues();
    }
}
