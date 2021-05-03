using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class InputDisplayLayout : MonoBehaviour
{
    public VerticalLayoutGroup RowContainer;
    public TextAsset LayoutFile;
    public Prototype LabelPrototype;
    public Prototype LinePrototype;
    //public UILineRendererList LineRenderer;
    public Prototype RowPrototype;
    public Prototype RowSpacerPrototype;
    public int KeySize = 64;
    public Color DefaultColor;
    public Color HighlightColor;
    public float FillBrightness = .5f;
    public float FillAlpha;
    public float FillMultiplier = .25f;
    public float InactiveMultiplier = .333f;
    public int TestSteps = 5;
    public float TestPadding = 4;
    public float BoxLineExpand = -1;
    public Color GlobalColor;
    public float GlobalHueBuffer = .1f;

    private Canvas _canvas;
    private Color _defaultInactiveColor;
    private InputLayout _inputLayout;
    private Dictionary<InputLayoutBindableKey, InputDisplayKey> _displayKeys = new Dictionary<InputLayoutBindableKey, InputDisplayKey>();
    private Dictionary<string, InputLayoutBindableKey> _bindKeys = new Dictionary<string, InputLayoutBindableKey>();
    private Dictionary<InputDisplayKey, InputAction> _actionMap = new Dictionary<InputDisplayKey, InputAction>();
    private Dictionary<InputAction, TextMeshProUGUI> _actionLabels = new Dictionary<InputAction, TextMeshProUGUI>();
    private Dictionary<InputAction, Color> _actionColors = new Dictionary<InputAction, Color>();
    private List<Rect> _reservedRects = new List<Rect>();
    private List<LayoutGroup> _layoutGroups = new List<LayoutGroup>();
    
    private string FullKeyPath(string shortPath) => $"<Keyboard>/{shortPath}";
    private string ShortKeyPath(string fullPath) => fullPath.Substring(fullPath.LastIndexOf('/') + 1);
    
    void Start()
    {
        _canvas = transform.root.GetComponent<Canvas>();
        _defaultInactiveColor = DefaultColor;
        _defaultInactiveColor.a *= InactiveMultiplier;
        var path = Path.Combine(ActionGameManager.GameDataDirectory.CreateSubdirectory("KeyboardLayouts").FullName, $"{LayoutFile.name}.msgpack");
        // _inputLayout = ParseJson(LayoutFile.text);
        RegisterResolver.Register();
        _inputLayout = MessagePackSerializer.Deserialize<InputLayout>(File.ReadAllBytes(path));
        DisplayLayout(_inputLayout);
        
        foreach (var key in _inputLayout.GetBindableKeys())
        {
            var fullInputPath = FullKeyPath(key.InputSystemPath);
            _bindKeys[fullInputPath] = key;
            // var keyPress = new InputAction(binding: fullInputPath);
            // keyPress.started += context =>
            // {
            //     var displayKey = _displayKeys[key];
            //     displayKey.Outline.color = HighlightColor;
            //     var fillColor = HighlightColor;
            //     fillColor *= FillMultiplier;
            //     fillColor.a = FillAlpha;
            //     displayKey.Fill.color = fillColor;
            // };
            // keyPress.canceled += context =>
            // {
            //     var displayKey = _displayKeys[key];
            //     displayKey.Outline.color = DefaultColor;
            //     var fillColor = DefaultColor;
            //     fillColor *= FillMultiplier;
            //     fillColor.a = FillAlpha;
            //     displayKey.Fill.color = fillColor;
            // };
            // keyPress.Enable();
        }

        var input = new AetheriaInput();
        // input.Global.Dock.performed += context => Debug.Log("Docking!");
        // input.Global.GalaxyMap.performed += context => 
        // {
        //     input.Global.Dock.Disable();
        //     var rebindOperation = input.Global.Dock.PerformInteractiveRebinding()
        //         .WithControlsExcluding("Mouse")
        //         .WithCancelingThrough("<Keyboard>/escape")
        //         .OnMatchWaitForAnother(0.2f)
        //         .Start();
        //     Debug.Log("Rebinding Dock!");
        //     rebindOperation.OnComplete(operation =>
        //     {
        //         Debug.Log("Rebinding Complete!");
        //         input.Global.Dock.Enable();
        //     });
        // };
        // input.Enable();
        
        _layoutGroups.Add(RowContainer);
        // foreach (var group in _layoutGroups)
        // {
        //     group.CalculateLayoutInputHorizontal();
        //     group.CalculateLayoutInputVertical();
        //     group.SetLayoutHorizontal();
        //     group.SetLayoutVertical();
        //     LayoutRebuilder.ForceRebuildLayoutImmediate(group.GetComponent<RectTransform>());
        // }
        // LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();

        Color.RGBToHSV(GlobalColor, out var hue, out var saturation, out var value);
        var actionIndex = 0;
        var actionCount = (float) input.asset.Count(a=>a.actionMap.name!="UI");
        foreach (var action in input.asset)
        {
            if(action.actionMap.name=="UI") continue;
            var h = frac(hue + GlobalHueBuffer + actionIndex++ / actionCount * (1 - GlobalHueBuffer * 2));
            if (action.actionMap.name == "Global")
            {
                h = hue;
                _actionColors[action] = GlobalColor;
            }
            else
                _actionColors[action] = Color.HSVToRGB(h, saturation, value);
            foreach (var binding in action.bindings)
            {
                if (_bindKeys.ContainsKey(binding.path))
                {
                    var displayKey = _displayKeys[_bindKeys[binding.path]];
                    displayKey.Fill.color = Color.HSVToRGB(h, saturation, FillBrightness);
                    displayKey.Outline.gameObject.SetActive(true);
                    displayKey.Outline.color = displayKey.MainLabel.color = displayKey.AltLabel.color = _actionColors[action];
                    _actionMap[displayKey] = action;
                    _reservedRects.Add(displayKey.GetComponent<RectTransform>().ScreenSpaceRect());
                }
            }
        }

        var center = _actionMap.Keys.Aggregate(Vector2.zero, (v, displayKey) => v + (Vector2) displayKey.transform.position) / _actionMap.Keys.Count;

        foreach (var x in _actionMap)
        {
            var key = x.Key;
            var action = x.Value;
            if(_actionLabels.ContainsKey(action)) continue;
            
            var label = LabelPrototype.Instantiate<TextMeshProUGUI>();
            label.color = _actionColors[action];
            _actionLabels[action] = label;
            var labelRect = label.rectTransform;
            label.text = _actionMap[key].name.Replace(' ', '\n');
            LayoutRebuilder.ForceRebuildLayoutImmediate(labelRect);
            var pos = (Vector2) key.transform.position;
            var dir = (pos - center).normalized;
            label.transform.position = FindLabelPosition(key.GetComponent<RectTransform>(), labelRect, dir);
        }
        
        foreach (var x in _actionMap)
        {
            var key = x.Key;
            var action = x.Value;
            
            var keyBounds = key.Outline.rectTransform.GetBounds(BoxLineExpand);
            var labelPoint = (Vector2) _actionLabels[action].rectTransform.GetBounds().ClosestPoint(key.Outline.transform.position);
            var keyPoint = (Vector2) keyBounds.ClosestPoint(labelPoint);

            var line = LinePrototype.Instantiate<UILineRenderer>();
            line.color = _actionColors[action];
            line.Points = new[] { keyPoint, labelPoint };
        }

        //StartCoroutine(AssociateInputKeys(_inputLayout));
    }

    private Vector2 FindLabelPosition(RectTransform key, RectTransform label, Vector2 dir)
    {
        var keyRect = key.ScreenSpaceRect();
        var labelRect = label.ScreenSpaceRect(TestPadding);
        var localRects = Overlap(key.ScreenSpaceRect(TestSteps * KeySize));
        var keyCenter = keyRect.center;
        for (int dist = KeySize; dist < TestSteps * KeySize; dist++)
        {
            for (float theta = 0; theta < PI * 2; theta += PI / 32)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    var testPos = keyCenter + dir.Rotate(theta * s) * dist;
                    var testRect = new Rect(testPos - labelRect.size / 2, labelRect.size);
                    if(!localRects.Any(r => r.Overlaps(testRect)))
                    {
                        _reservedRects.Add(testRect);
                        return testPos;
                    }
                }
            }
        }
        return Vector2.zero;
    }
    
    private Rect[] Overlap(Rect rect) => _reservedRects.Where(r => r.Overlaps(rect)).ToArray();

    public void DisplayLayout(InputLayout layout)
    {
        foreach (var row in layout.Rows)
        {
            if (row is InputLayoutKeyRow keyRow)
            {
                var displayRow = RowPrototype.Instantiate<InputDisplayRow>();
                _layoutGroups.Add(displayRow.LayoutGroup);
                foreach (var column in keyRow.Columns)
                {
                    if (column is InputLayoutColumnSpacer)
                    {
                        displayRow.KeySpacerPrototype.Instantiate<LayoutElement>().preferredWidth = KeySize * column.Width;
                    }
                    else if (column is InputLayoutKey)
                    {
                        var displayKey = displayRow.KeyPrototype.Instantiate<InputDisplayKey>();
                        displayKey.LayoutElement.preferredHeight = KeySize;
                        displayKey.LayoutElement.preferredWidth = KeySize * column.Width;
                        displayKey.Outline.gameObject.SetActive(false);
                        displayKey.Outline.color = DefaultColor;
                        var fillColor = DefaultColor;
                        fillColor *= FillMultiplier;
                        fillColor.a = FillAlpha;
                        displayKey.MainLabel.color = displayKey.AltLabel.color = Color.Lerp(_defaultInactiveColor, fillColor, .5f);

                        if (column is InputLayoutBindableKey key)
                        {
                            _displayKeys[key] = displayKey;
                            displayKey.MainLabel.text = key.MainLabel;
                            displayKey.AltLabel.text = key.AltLabel;

                            if (column is InputLayoutMultiRowKey multiRowKey)
                            {
                                displayKey.Outline.rectTransform.anchorMin =
                                    displayKey.MainLabel.rectTransform.anchorMin =
                                        displayKey.AltLabel.rectTransform.anchorMin =
                                            displayKey.Fill.rectTransform.anchorMin = Vector2.down * (multiRowKey.Height - 1);
                            }
                        }
                        else
                        {
                            fillColor.a *= InactiveMultiplier;
                            displayKey.MainLabel.text = "";
                            displayKey.AltLabel.text = "";
                        }
                        displayKey.Fill.color = fillColor;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(column));
                    }
                }
            }
            else if (row is InputLayoutRowSpacer rowSpacer)
            {
                RowSpacerPrototype.Instantiate<LayoutElement>().preferredHeight = KeySize * rowSpacer.Height;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
        }
    }

    public InputLayout ParseJson(string layout)
    {
        var rows = new List<InputLayoutRow>();
        var nextWidth = 1f;
        var nextHeight = 1;
        var reader = new JsonTextReader(new StringReader(layout));
        
        reader.Read();
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonReaderException($"Unexpected JSON format in line {reader.LineNumber}:{reader.LinePosition}, expected: StartArray, received: {Enum.GetName(typeof(JsonToken), reader.TokenType)}");
        
        while(reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonReaderException($"Unexpected JSON format in line {reader.LineNumber}:{reader.LinePosition}, expected: StartArray, received: {Enum.GetName(typeof(JsonToken), reader.TokenType)}");

            var row = new InputLayoutKeyRow();
            var columns = new List<InputLayoutColumn>();

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        while(reader.Read() && reader.TokenType != JsonToken.EndObject)
                        {
                            switch (reader.Value.ToString())
                            {
                                case "a": break;
                                case "w":
                                    nextWidth = (float) (reader.ReadAsDouble() ?? 1.0);
                                    break;
                                case "h":
                                    nextHeight = reader.ReadAsInt32() ?? 1;
                                    break;
                                case "y":
                                    rows.Add(new InputLayoutRowSpacer{Height = (float) (reader.ReadAsDouble() ?? 0.0)});
                                    break;
                                case "x":
                                    columns.Add(new InputLayoutColumnSpacer{Width = (float) (reader.ReadAsDouble() ?? 0.0)});
                                    break;
                            }
                        }
                        break;
                    case JsonToken.String:
                        InputLayoutKey key;
                        var v = reader.Value.ToString().Trim();
                        if (!string.IsNullOrEmpty(v))
                        {
                            var labels = v.Split('\n');
                            key = nextHeight != 1 ? new InputLayoutMultiRowKey {Height = nextHeight} : new InputLayoutBindableKey();
                            nextHeight = 1;
                            ((InputLayoutBindableKey) key).MainLabel = labels.Length == 2 ? labels[1] : v;
                            ((InputLayoutBindableKey) key).AltLabel = labels.Length == 2 ? labels[0] : "";
                        }
                        else
                            key = new InputLayoutKey();

                        key.Width = nextWidth;
                        columns.Add(key);
                        nextWidth = 1f;
                        break;
                    default:
                        throw new JsonReaderException($"Unexpected JSON format in line {reader.LineNumber}:{reader.LinePosition}");
                }
            }

            row.Columns = columns.ToArray();
            rows.Add(row);
        }

        return new InputLayout {Rows = rows.ToArray()};
    }

    private IEnumerator AssociateInputKeys(InputLayout layout)
    {
        var path = "";
        var keyPress = new InputAction(binding: "/<Keyboard>/<button>");
        keyPress.performed += context =>
        {
            var keyName = ShortKeyPath(context.control.path);
            if (keyName == "anyKey") return;
            path = keyName;
        };
        keyPress.Enable();
        foreach (var row in layout.Rows)
        {
            if (row is InputLayoutKeyRow keyRow)
            {
                foreach (var column in keyRow.Columns)
                {
                    if(column is InputLayoutBindableKey bindableKey)
                    {
                        _displayKeys[bindableKey].Outline.color = HighlightColor;
                        var fillColor = HighlightColor;
                        fillColor *= FillMultiplier;
                        fillColor.a = FillAlpha;
                        _displayKeys[bindableKey].Fill.color = fillColor;
                        
                        path = "";
                        while (string.IsNullOrEmpty(path)) yield return null;
                        bindableKey.InputSystemPath = path;
                        Debug.Log($"Bound \"{path}\" to \"{bindableKey.MainLabel}\"");
                        
                        _displayKeys[bindableKey].Outline.color = DefaultColor;
                        fillColor = DefaultColor;
                        fillColor *= FillMultiplier;
                        fillColor.a = FillAlpha;
                        _displayKeys[bindableKey].Fill.color = fillColor;
                    }
                }
            }
        }
        keyPress.Disable();

        RegisterResolver.Register();
        File.WriteAllBytes(
            Path.Combine(ActionGameManager.GameDataDirectory.CreateSubdirectory("KeyboardLayouts").FullName, $"{LayoutFile.name}.msgpack"),
            MessagePackSerializer.Serialize(layout));
    }

    void Update()
    {
        
    }
}
