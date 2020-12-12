using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using System.IO;
 
// https://forum.unity.com/threads/sprite-editor-automatic-slicing-by-script.320776/
// http://www.sarpersoher.com/a-custom-asset-importer-for-unity/
// public class SpritePostprocessor : AssetPostprocessor {
//     public void OnPostprocessTexture(Texture2D texture) {
//         TextureImporter importer = assetImporter as TextureImporter;
//         if (importer.spriteImportMode != SpriteImportMode.Multiple) {
//             return;
//         }
//  
//         Debug.Log("OnPostprocessTexture generating sprites");
//  
//         int minimumSpriteSize = 8;
//         int extrudeSize = 0;
//
//         Rect[] rects = InternalSpriteUtility.GenerateGridSpriteRectangles(texture, Vector2.one, Vector2.one * 8, Vector2.one * 2, true);
//         //InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, minimumSpriteSize, extrudeSize);
//         // List<Rect> rectsList = new List<Rect>(rects);
//         // rectsList = SortRects(rectsList, texture.width);
//  
//         string filenameNoExtension = Path.GetFileNameWithoutExtension(assetPath);
//         List<SpriteMetaData> metas = new List<SpriteMetaData>();
//         int rectNum = 0;
//  
//         foreach (Rect rect in rects) {
//             SpriteMetaData meta = new SpriteMetaData();
//             meta.border = Vector4.one * 4;
//             meta.rect = rect;
//             meta.name = filenameNoExtension + "_" + rectNum++;
//             metas.Add(meta);
//         }
//  
//         importer.spritesheet = metas.ToArray();
//     }
// }
 