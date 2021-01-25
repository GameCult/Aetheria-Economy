using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class SpreadsheetRow : MonoBehaviour
{
    public Prototype Column;
    public Image Background;
    public ObservablePointerClickTrigger ClickTrigger;

    private List<RectTransform> _entries = new List<RectTransform>();

    public void ShowData(SpreadsheetEntryRow data)
    {
        foreach(var entry in _entries)
            entry.GetComponent<Prototype>().ReturnToPool();
        _entries.Clear();

        foreach (var column in data.Columns)
        {
            var entry = Column.Instantiate<RectTransform>();
            entry.GetComponent<TextMeshProUGUI>().text = column.Output();
            _entries.Add(entry);
        }
    }

    public void ApplyColumnSizes(int[] sizes)
    {
        var distance = 0;
        for (int i = 0; i < sizes.Length; i++)
        {
            _entries[i].anchoredPosition = Vector2.right * distance;
            _entries[i].sizeDelta = Vector2.right * sizes[i];
            distance += sizes[i];
        }
    }
}
