using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TradeMenu : MonoBehaviour
{
    public ActionGameManager GameManager;
    public DropdownMenu Dropdown;
    public Button NewFilterButton;
    public SizeFilter MinimumSizeFilter;
    public SizeFilter MaximumSizeFilter;
    public RectTransform ItemList;
    public PropertiesPanel Properties;
    public Spreadsheet Spreadsheet;

    private void OnEnable()
    {
        Properties.Context = GameManager.ItemManager;
        Spreadsheet.ShowData(
            new[] {"Name", "Mass"},
            new[] {2, 1},
            GameManager.ItemManager.ItemData.GetAll<ItemData>().Select(i => new SpreadsheetEntryRow
            {
                Columns = new[]
                {
                    new SpreadsheetEntryColumn
                    {
                        Output = () => i.Name,
                        SortKey = i.Name
                    },
                    new SpreadsheetEntryColumn
                    {
                        Output = () => i.Mass.SignificantDigits(3),
                        SortKey = i.Mass
                    },
                },
                OnClick = () =>
                {
                    Properties.Clear();
                    Properties.AddItemDataProperties(i);
                }
            }));
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
