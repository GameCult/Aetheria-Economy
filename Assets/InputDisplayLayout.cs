using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class InputDisplayLayout : MonoBehaviour
{
    public TextAsset LayoutFile;
    public Prototype RowPrototype;
    public Prototype RowSpacerPrototype;
    public int KeySize = 64;
    
    void Start()
    {
        var nextWidth = 1f;
        var nextHeight = 1f;
        var reader = new JsonTextReader(new StringReader(LayoutFile.text));
        reader.Read();
        if (reader.TokenType != JsonToken.StartArray)
        {
            Debug.Log($"Unexpected JSON format in line {reader.LineNumber}:{reader.LinePosition}");
            return;
        }
        while(reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                Debug.Log($"Unexpected JSON format in line {reader.LineNumber}:{reader.LinePosition}");
                return;
            }

            var row = RowPrototype.Instantiate<InputDisplayRow>();

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
                                    nextHeight = (float) (reader.ReadAsDouble() ?? 1.0);
                                    break;
                                case "y":
                                    RowSpacerPrototype.Instantiate<LayoutElement>().preferredHeight = KeySize * (float) (reader.ReadAsDouble() ?? 0.0);
                                    row.transform.SetAsLastSibling();
                                    break;
                                case "x":
                                    row.KeySpacerPrototype.Instantiate<LayoutElement>().preferredWidth = KeySize * (float) (reader.ReadAsDouble() ?? 0.0);
                                    break;
                            }
                        }
                        break;
                    case JsonToken.String:
                        var key = row.KeyPrototype.Instantiate<InputDisplayKey>();
                        var v = reader.Value.ToString().Trim();
                        if (!string.IsNullOrEmpty(v))
                        {
                            var labels = v.Split('\n');
                            if (labels.Length == 2)
                            {
                                key.First.text = labels[1];
                                key.Second.text = labels[0];
                            }
                            else
                            {
                                key.First.text = v;
                                key.Second.text = "";
                            }
                        }
                        else
                        {
                            key.First.text = "";
                            key.Second.text = "";
                        }

                        key.LayoutElement.preferredHeight = KeySize;
                        key.LayoutElement.preferredWidth = KeySize * nextWidth;
                        nextWidth = 1f;
                        if (Math.Abs(nextHeight - 1f) > .01f)
                        {
                            key.Outline.rectTransform.anchorMin =
                                key.First.rectTransform.anchorMin =
                                    key.Second.rectTransform.anchorMin =
                                        key.Fill.rectTransform.anchorMin = Vector2.down * (nextHeight-1);
                        }
                        nextHeight = 1f;
                        break;
                    default:
                        Debug.Log($"Unexpected JSON format in line {reader.LineNumber}:{reader.LinePosition}");
                        return;
                }
            }
        }
    }

    void Update()
    {
        
    }
}
