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
    public RectTransform UnassignedBindingsGroup;
    public InputDisplayButton MouseLeft;
    public InputDisplayButton MouseRight;
    public InputDisplayButton MouseMiddle;
    public InputDisplayButton MouseForward;
    public InputDisplayButton MouseBack;
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
    public int PlacementSearchArea = 4;
    public float TestPadding = 4;
    public float BoxLineExpand = -1;
    public float GlobalHue = .333f;
    public float Saturation;
    public float GlobalHueRange = .1f;

    private Canvas _canvas;
    private InputLayout _inputLayout;

    private class ButtonMapping
    {
        public IBindableButton Button;
        public InputDisplayButton DisplayButton;
        public ActionMapping ActionMapping;
        public Rect ButtonRect;
        public UILineRenderer LabelLine;
    }

    private class ActionMapping
    {
        public InputAction Action;
        public TextMeshProUGUI Label;
        public Rect LabelRect;
        public float Hue;
        public List<ButtonMapping> ButtonMappings = new List<ButtonMapping>();
    }
    
    private Dictionary<string, ButtonMapping> _bindButtons = new Dictionary<string, ButtonMapping>();
    private List<ButtonMapping> _buttonMappings = new List<ButtonMapping>();
    private List<ActionMapping> _actionMappings = new List<ActionMapping>();
    
    void Start()
    {
        _canvas = transform.root.GetComponent<Canvas>();
        var path = Path.Combine(ActionGameManager.GameDataDirectory.CreateSubdirectory("KeyboardLayouts").FullName, $"{LayoutFile.name}.msgpack");
        // _inputLayout = ParseJson(LayoutFile.text);
        RegisterResolver.Register();
        _inputLayout = MessagePackSerializer.Deserialize<InputLayout>(File.ReadAllBytes(path));
        DisplayLayout(_inputLayout);

        foreach (var buttonMapping in _buttonMappings) _bindButtons[buttonMapping.Button.InputSystemPath] = buttonMapping;

        // foreach (var key in _inputLayout.GetBindableKeys())
        // {
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
        // }
        
        Canvas.ForceUpdateCanvases();

        foreach (var buttonMapping in _buttonMappings)
            buttonMapping.ButtonRect = buttonMapping.DisplayButton.GetComponent<RectTransform>().ScreenSpaceRect();
        
        var input = new AetheriaInput();
        ProcessActions(input.asset);

        foreach (var actionMapping in _actionMappings)
        {
            actionMapping.Label = CreateLabel(actionMapping);
            PlaceLabel(actionMapping);
            foreach(var buttonMapping in actionMapping.ButtonMappings) ConnectButtonToLabel(buttonMapping);
        }

        //StartCoroutine(AssociateInputKeys(_inputLayout));
    }

    private void ProcessActions(InputActionAsset input)
    {
        var nonUIActions = input.Where(a => a.actionMap.name != "UI").ToArray();
        for (var i = 0; i < nonUIActions.Length; i++)
        {
            var action = nonUIActions[i];
            var actionMapping = new ActionMapping
            {
                Action = action,
                Hue = action.actionMap.name == "Global"
                    ? GlobalHue
                    : frac(GlobalHue + GlobalHueRange + (float) i / (nonUIActions.Length - 1) * (1 - GlobalHueRange * 2))
            };
            _actionMappings.Add(actionMapping);
            foreach (var binding in action.bindings)
            {
                if (_bindButtons.ContainsKey(binding.path))
                {
                    var buttonMapping = _bindButtons[binding.path];
                    buttonMapping.ActionMapping = actionMapping;
                    AssignColor(buttonMapping);
                    actionMapping.ButtonMappings.Add(buttonMapping);
                }
            }
        }
    }

    private void AssignColor(ButtonMapping buttonMapping)
    {
        if(buttonMapping.ActionMapping!=null)
        {
            var outlineColor = Color.HSVToRGB(buttonMapping.ActionMapping.Hue, Saturation, 1);
            var fillColor = Color.HSVToRGB(buttonMapping.ActionMapping.Hue, 1, FillBrightness);
            fillColor.a = FillAlpha;
            buttonMapping.DisplayButton.Fill.color = fillColor;
            buttonMapping.DisplayButton.Outline.gameObject.SetActive(true);
            buttonMapping.DisplayButton.Outline.color = outlineColor;
            if (buttonMapping.DisplayButton is InputDisplayKey key) key.MainLabel.color = key.AltLabel.color = outlineColor;
        }
        else AssignUnboundColor(buttonMapping.DisplayButton);
    }

    private void AssignUnboundColor(InputDisplayButton button, bool bindable = true)
    {
        button.Outline.gameObject.SetActive(false);
        var outlineColor = DefaultColor;
        outlineColor.a *= InactiveMultiplier;
        var fillColor = DefaultColor * FillBrightness;
        fillColor *= FillBrightness;
        if(!bindable)
            fillColor *= InactiveMultiplier;
        fillColor.a = FillAlpha;
        button.Fill.color = fillColor;
        if (button is InputDisplayKey key) key.MainLabel.color = key.AltLabel.color = Color.Lerp(outlineColor, fillColor, .5f);
    }

    private TextMeshProUGUI CreateLabel(ActionMapping actionMapping)
    {
        var label = LabelPrototype.Instantiate<TextMeshProUGUI>();
        label.color = Color.HSVToRGB(actionMapping.Hue, Saturation, 1);
        actionMapping.Label = label;
        label.text = actionMapping.Action.name.Replace(' ', '\n');
        return label;
    }

    private void PlaceLabel(ActionMapping actionMapping)
    {
        if(actionMapping.ButtonMappings.Any())
        {
            var labelRect = actionMapping.Label.rectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(labelRect);
            var rect = FindLabelPosition(actionMapping.ButtonMappings.First().DisplayButton.GetComponent<RectTransform>(), labelRect);
            actionMapping.LabelRect = rect;
            actionMapping.Label.transform.position = rect.center;
        }
        else
        {
            actionMapping.Label.rectTransform.parent = UnassignedBindingsGroup;
        }
    }

    private void ConnectButtonToLabel(ButtonMapping buttonMapping)
    {
        if (buttonMapping.ActionMapping != null)
        {
            var keyBounds = buttonMapping.DisplayButton.Outline.rectTransform.GetBounds(BoxLineExpand);
            var labelPoint = (Vector2) buttonMapping.ActionMapping.Label.rectTransform.GetBounds().ClosestPoint(keyBounds.center);
            var keyPoint = (Vector2) keyBounds.ClosestPoint(labelPoint);
            labelPoint /= _canvas.scaleFactor;
            keyPoint /= _canvas.scaleFactor;

            buttonMapping.LabelLine ??= LinePrototype.Instantiate<UILineRenderer>();
            buttonMapping.LabelLine.color = buttonMapping.ActionMapping.Label.color;
            buttonMapping.LabelLine.Points = new[] {keyPoint, labelPoint};
        }
        else
        {
            buttonMapping.LabelLine?.GetComponent<Prototype>().ReturnToPool();
            buttonMapping.LabelLine = null;
        }
    }

    private Rect FindLabelPosition(RectTransform key, RectTransform label)
    {
        var keyRect = key.ScreenSpaceRect();
        var labelRect = label.ScreenSpaceRect(TestPadding*_canvas.scaleFactor);
        var localRects = Overlap(key.ScreenSpaceRect(PlacementSearchArea * KeySize * _canvas.scaleFactor));
        var centroid = localRects.Aggregate(Vector2.zero, (v, r) => v + r.center) / localRects.Length;
        var keyCenter = keyRect.center;
        var dir = (keyCenter - centroid).normalized;
        for (int dist = KeySize; dist < PlacementSearchArea * KeySize; dist++)
        {
            for (float theta = 0; theta < PI * 2; theta += PI / 32)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    var testPos = keyCenter + dir.Rotate(theta * s) * dist * _canvas.scaleFactor;
                    var testRect = new Rect(testPos - labelRect.size / 2, labelRect.size);
                    if(!localRects.Any(r => r.Overlaps(testRect)))
                    {
                        return testRect;
                    }
                }
            }
        }
        return default;
    }
    
    private Rect[] Overlap(Rect rect) => _buttonMappings
        .Where(b=>b.ActionMapping!=null)
        .Select(b=>b.ButtonRect)
        .Concat(_actionMappings
            .Where(a=>a.LabelRect.width > .1f)
            .Select(a=>a.LabelRect))
        .Where(r => r.Overlaps(rect)).ToArray();

    public void DisplayLayout(InputLayout layout)
    {
        foreach (var row in layout.Rows)
        {
            if (row is InputLayoutKeyRow keyRow)
            {
                var displayRow = RowPrototype.Instantiate<InputDisplayRow>();
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
                        AssignUnboundColor(displayKey, column is InputLayoutBindableKey);

                        if (column is InputLayoutBindableKey key)
                        {
                            var buttonMapping = new ButtonMapping
                            {
                                Button = key, 
                                DisplayButton = displayKey
                            };
                            _bindButtons[key.InputSystemPath] = buttonMapping;
                            _buttonMappings.Add(buttonMapping);
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
                            displayKey.MainLabel.text = "";
                            displayKey.AltLabel.text = "";
                        }
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

    private IEnumerator AssociateInputKeys()
    {
        var path = "";
        var keyPress = new InputAction(binding: "/<Keyboard>/<button>");
        keyPress.performed += context =>
        {
            if (context.control.path.EndsWith("anyKey")) return;
            path = context.control.path;
        };
        keyPress.Enable();
        foreach (var button in _buttonMappings)
        {
            button.DisplayButton.Outline.color = HighlightColor;
            var fillColor = HighlightColor;
            fillColor *= FillMultiplier;
            fillColor.a = FillAlpha;
            button.DisplayButton.Fill.color = fillColor;
                        
            path = "";
            while (string.IsNullOrEmpty(path)) yield return null;
            button.Button.InputSystemPath = path;
            //Debug.Log($"Bound \"{path}\" to \"{bindableKey.MainLabel}\"");
                        
            button.DisplayButton.Outline.color = DefaultColor;
            fillColor = DefaultColor;
            fillColor *= FillMultiplier;
            fillColor.a = FillAlpha;
            button.DisplayButton.Fill.color = fillColor;
        }
        keyPress.Disable();

        RegisterResolver.Register();
        File.WriteAllBytes(
            Path.Combine(ActionGameManager.GameDataDirectory.CreateSubdirectory("KeyboardLayouts").FullName, $"{LayoutFile.name}.msgpack"),
            MessagePackSerializer.Serialize(_inputLayout));
    }

    void Update()
    {
        
    }
}
