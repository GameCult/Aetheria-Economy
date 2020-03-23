using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using MIConvexHull;
using UniRx;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Galaxy))]
public class GalaxyEditor : Editor
{
	private int _width = 0;
	private bool _showResourceMaps;
	private GalaxyMapLayerData _currentLayer;
	private string _currentLayerName = "Star Density";
	private bool _showStarEditor;
	private int _hilbertOrder = 6;
	private ulong _hilbertIndex;
	private int _starCount = 1;
	private float _maxLinkLength = .1f;
	private float _linkFilter = .5f;
	private IEnumerable<Vector2> _stars = new Vector2[0];
	private bool _drawStars;
	private bool _drawResource;
	private bool _drawLinks;
	private VoronoiMesh< Vertex2, Cell2, VoronoiEdge<Vertex2, Cell2>> _voronoiMesh;
	private List<VoronoiLink> _starLinks = new List<VoronoiLink>();
	private Texture2D _white;
	private Texture2D _starTex;
	private Texture2D _linkTex;
	private Material _galaxyMat;
	private Material _transparent;

	private void OnEnable()
	{
		_white = Color.white.ToTexture();
		_galaxyMat = new Material(Shader.Find("Unlit/GalaxyMap"));
		_transparent = new Material(Shader.Find("Unlit/Transparent"));
		_width = Screen.width;
		_starTex = new Texture2D(_width,_width, TextureFormat.ARGB32, false);
		_linkTex = new Texture2D(_width,_width, TextureFormat.ARGB32, false);
	}

	public override void OnInspectorGUI()
	{
		var galaxy = target as Galaxy;

		if (_currentLayer == null)
			_currentLayer = galaxy.MapData.StarDensity;
		
		if (Screen.width != _width)
		{
			_width = Screen.width;
			_starTex = new Texture2D(_width,_width, TextureFormat.ARGB32, false);
			_linkTex = new Texture2D(_width,_width, TextureFormat.ARGB32, false);
			RenderStars();
			RenderLinks();
		}
		
		GUILayout.Label("Preview", EditorStyles.boldLabel);
		_galaxyMat.SetFloat("Arms", galaxy.MapData.Arms);
		_galaxyMat.SetFloat("Twist", galaxy.MapData.Twist);
		_galaxyMat.SetFloat("TwistPower", galaxy.MapData.TwistPower);
		_galaxyMat.SetFloat("Arms", galaxy.MapData.Arms);
		_galaxyMat.SetFloat("SpokeOffset", _currentLayer.SpokeOffset);
		_galaxyMat.SetFloat("SpokeScale", _currentLayer.SpokeScale);
		_galaxyMat.SetFloat("CoreBoost", _currentLayer.CoreBoost);
		_galaxyMat.SetFloat("CoreBoostOffset", _currentLayer.CoreBoostOffset);
		_galaxyMat.SetFloat("CoreBoostPower", _currentLayer.CoreBoostPower);
		_galaxyMat.SetFloat("EdgeReduction", _currentLayer.EdgeReduction);
		_galaxyMat.SetFloat("NoisePosition", _currentLayer.NoisePosition);
		_galaxyMat.SetFloat("NoiseAmplitude", _currentLayer.NoiseAmplitude);
		_galaxyMat.SetFloat("NoiseOffset", _currentLayer.NoiseOffset);
		_galaxyMat.SetFloat("NoiseGain", _currentLayer.NoiseGain);
		_galaxyMat.SetFloat("NoiseLacunarity", _currentLayer.NoiseLacunarity);
		_galaxyMat.SetFloat("NoiseFrequency", _currentLayer.NoiseFrequency);
		var rect = EditorGUILayout.GetControlRect(false, _width);
		EditorGUI.DrawPreviewTexture(rect, _white, _galaxyMat);
		if(_drawLinks)
			EditorGUI.DrawPreviewTexture(rect, _linkTex, _transparent);
		if(_drawStars)
			EditorGUI.DrawPreviewTexture(rect, _starTex, _transparent);
		
		_drawStars = EditorGUILayout.ToggleLeft($"Display {_stars.Count()} Stars", _drawStars);
		_drawResource = EditorGUILayout.ToggleLeft("Display Resources", _drawResource);
		_drawLinks = EditorGUILayout.ToggleLeft($"Display {_starLinks.Count} Links", _drawLinks);
		
		GUILayout.Space(10);
		
		// Show default inspector property editor
		DrawDefaultInspector ();

		EditorGUILayout.BeginVertical("Box");
		EditorGUI.indentLevel++;
		GUILayout.Label(_currentLayerName);
		Inspect(_currentLayer);
		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
		
		EditorGUILayout.BeginVertical("Box");
		EditorGUI.indentLevel++;
		_showResourceMaps = EditorGUILayout.Foldout(_showResourceMaps, "Resource Density Maps");
		if (_showResourceMaps)
		{
			EditorGUI.indentLevel++;
			foreach (var resourceDensity in galaxy.MapData.ResourceDensities.ToArray())
			{
				EditorGUILayout.BeginHorizontal();
				var newName = EditorGUILayout.DelayedTextField(resourceDensity.Key);
				if (newName != resourceDensity.Key)
				{
					galaxy.MapData.ResourceDensities.Remove(resourceDensity.Key);
					galaxy.MapData.ResourceDensities[newName] = resourceDensity.Value;
				}
				if (GUILayout.Button("Inspect"))
				{
					_currentLayerName = resourceDensity.Key;
					_currentLayer = resourceDensity.Value;
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUI.indentLevel--;
			if (GUILayout.Button("Add New Resource"))
			{
				galaxy.MapData.ResourceDensities["New Resource"] = new GalaxyMapLayerData();
			}
		}
		else if(_currentLayer != galaxy.MapData.StarDensity)
		{
			_currentLayerName = "Star Density";
			_currentLayer = galaxy.MapData.StarDensity;
		}
		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
		
		EditorGUILayout.BeginVertical("Box");
		EditorGUI.indentLevel++;
		_showStarEditor = EditorGUILayout.Foldout(_showStarEditor, "Star Tools");
		if (_showStarEditor)
		{
			_hilbertOrder = EditorGUILayout.IntField("Hilbert Order", _hilbertOrder);
			_starCount = EditorGUILayout.IntField("Star Count", _starCount);
			//_hilbertIndex = (ulong) EditorGUILayout.IntField("Hilbert Index", (int) _hilbertIndex);
			if (GUILayout.Button("Evaluate Hilbert Curve"))
			{
				var points = EvaluateHilbert(_hilbertOrder,false);
				Debug.Log($"Hilbert curve has {points.Count()} points, resolution {Mathf.RoundToInt(points.Max(p=>p.x))+1}");
			}
			if (GUILayout.Button("Generate Stars"))
			{
				var points = EvaluateHilbert(_hilbertOrder).ToArray();
				var stars = new List<Vector2>();
				int bail = 0;
				while (stars.Count < _starCount && bail < 10)
				{
					var accum = 0f;
					foreach (var hp in points.Select(p=>p+UnityEngine.Random.insideUnitCircle * ((points[0]-points[1]).magnitude/2)))
					{
						var den = galaxy.MapData.StarDensity.Evaluate(hp, galaxy.MapData);
						if(!float.IsNaN(den))
							accum += den * UnityEngine.Random.value;
//						else
//							Debug.Log($"Density at ({hp.x},{hp.y}) is NaN");
						if (accum > 1)
						{
							stars.Add(hp);
							accum = 0;
						}
						//Debug.Log($"Accumulator: {accum}");
					}
					bail++;
				}
				Debug.Log($"Generated {stars.Count} stars.");
				_stars = stars;
				RenderStars();
			}
			if (_stars.Any())
			{
				_maxLinkLength = EditorGUILayout.FloatField("Max Link Length", _maxLinkLength);
				if (GUILayout.Button("Generate Star Links"))
				{
					_voronoiMesh = VoronoiMesh<Vertex2, Cell2, VoronoiEdge<Vertex2, Cell2>>.Create(_stars.Select(s=>new Vertex2(s.x,s.y)).ToList());
					_starLinks.Clear();
					// Each cell in this collection represents one of the triangle faces of the Delaunay Triangulation
					foreach (var cell in _voronoiMesh.Vertices)
					{
						var links = new[] {new VoronoiLink(cell.Vertices[0], cell.Vertices[1]),
											new VoronoiLink(cell.Vertices[0], cell.Vertices[2]),
											new VoronoiLink(cell.Vertices[2], cell.Vertices[1])};
						
						_starLinks.AddRange(links.Where(l=>!_starLinks.ContainsLine(l) && l.Length < _maxLinkLength));
						RenderLinks();
					}
				}
				if (_starLinks.Any())
				{
					_linkFilter = EditorGUILayout.FloatField("Link Filter Percentage", _linkFilter);
					if (GUILayout.Button("Filter Star Links"))
					{
						var bail = 0;
						var count = _starLinks.Count * Mathf.Clamp01(_linkFilter);
						var deadLinks = new List<VoronoiLink>();
						for (int i = 0; i < count && bail < count*10; bail++)
						{
							var link = _starLinks.ElementAt(UnityEngine.Random.Range(0, _starLinks.Count));
							
							if (deadLinks.Contains(link)) continue;
							
							var mapMinusLink = _starLinks.Where(l => !l.Equals(link)).ToArray();
							if (!mapMinusLink.ConnectedRegion(link.point1).Contains(link.point2))
								deadLinks.Add(link);
							else
							{
								_starLinks.Remove(link);
								i++;
							}
							//if (_starLinks.Count(sl => sl.ContainsPoint(link.point1)) > 1 && _starLinks.Count(sl => sl.ContainsPoint(link.point2)) > 1)
						}
						RenderLinks();
					}
					if (GUILayout.Button("Save Star Data"))
					{
						galaxy.MapData.Stars = _stars.Select(s => new StarData {Position = s}).ToList();
						foreach (var star in galaxy.MapData.Stars)
						{
							star.Links.Clear();
							star.Links.AddRange(_starLinks.Where(sl => (sl.point1.ToVector2() - star.Position).sqrMagnitude < float.Epsilon)
								.Select(sl => galaxy.MapData.Stars.IndexOf(galaxy.MapData.Stars.First(s => (sl.point2.ToVector2() - s.Position).sqrMagnitude < float.Epsilon))));
							star.Links.AddRange(_starLinks.Where(sl => (sl.point2.ToVector2() - star.Position).sqrMagnitude < float.Epsilon)
								.Select(sl => galaxy.MapData.Stars.IndexOf(galaxy.MapData.Stars.First(s => (sl.point1.ToVector2() - s.Position).sqrMagnitude < float.Epsilon))));
						}
					}
				}
			}
		}
		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
		
		
		EditorUtility.SetDirty(target);
	}

	private IEnumerable<Vector2> EvaluateHilbert(int order, bool normalized = true)
	{
		var points = new List<Vector2>();
		uint x = 0, y = 1;
		ulong i = 1;
		uint max = 0;
		while (x != 0 || y != 0)
		{
			Microsoft.SqlServer.Types.SpaceFillingCurve.ReverseHilbert(order, i, out x, out y);
			points.Add(new Vector2(x, y));
			if (x > max)
				max = x;
			i++;
		}
		return normalized ? points.Select(p => p / (max + 1)) : points;
	}

	void Inspect(GalaxyMapLayerData layer)
	{
		layer.EdgeReduction = EditorGUILayout.FloatField("Edge Reduction", layer.EdgeReduction);
		layer.CoreBoost = EditorGUILayout.FloatField("Core Boost", layer.CoreBoost);
		layer.CoreBoostOffset = EditorGUILayout.FloatField("Core Boost Offset", layer.CoreBoostOffset);
		layer.CoreBoostPower = EditorGUILayout.FloatField("Core Boost Power", layer.CoreBoostPower);
		layer.NoiseOffset = EditorGUILayout.FloatField("Noise Offset", layer.NoiseOffset);
		layer.NoiseAmplitude = EditorGUILayout.FloatField("Noise Amplitude", layer.NoiseAmplitude);
		layer.NoiseGain = EditorGUILayout.FloatField("Noise Gain", layer.NoiseGain);
		layer.NoiseLacunarity = EditorGUILayout.FloatField("Noise Lacunarity", layer.NoiseLacunarity);
		layer.NoiseOctaves = EditorGUILayout.IntField("Noise Octaves", layer.NoiseOctaves);
		layer.NoiseFrequency = EditorGUILayout.FloatField("Noise Frequency", layer.NoiseFrequency);
		layer.SpokeOffset = EditorGUILayout.FloatField("Spoke Offset", layer.SpokeOffset);
		layer.SpokeScale = EditorGUILayout.FloatField("Spoke Scale", layer.SpokeScale);
	}
	
	int UvToIndex(Vector2 v) => (int) (v.y * _width) * _width + (int) (v.x * _width);

	void RenderLinks()
	{
		Color[] pixels = new Color[_width*_width];
		for (var i = 0; i < pixels.Length; i++)
		{
			pixels[i] = Color.clear;
		}
		foreach (var l in _starLinks)
		{
			var pixelCount = Mathf.Max((int) (Mathf.Abs((float) (l.point1.x - l.point2.x)) * _width), (int) (Mathf.Abs((float) (l.point1.y - l.point2.y)) * _width));
			//Debug.Log($"Marching across {pixelCount} pixels!");
			for(int i=0;i<pixelCount;i++)
				pixels[UvToIndex(Vector2.Lerp(l.point1.ToVector2(),l.point2.ToVector2(),(float)i/pixelCount))] = XKCDColors.OrangeRed;
		}
		_linkTex.SetPixels(pixels);
		_linkTex.Apply();
	}

	void RenderStars()
	{
		Color[] pixels = new Color[_width*_width];
		for (var i = 0; i < pixels.Length; i++)
		{
			pixels[i] = Color.clear;
		}
		foreach (var s in _stars)
		{
			pixels[UvToIndex(s)] = Color.white;
			pixels[UvToIndex(s + Vector2.up / _width)] = new Color(.5f, .5f, .5f, 1);
			pixels[UvToIndex(s - Vector2.up / _width)] = new Color(.5f, .5f, .5f, 1);
			pixels[UvToIndex(s + Vector2.right / _width)] = new Color(.5f, .5f, .5f, 1);
			pixels[UvToIndex(s - Vector2.right / _width)] = new Color(.5f, .5f, .5f, 1);
			pixels[UvToIndex(s + Vector2.up / _width + Vector2.right / _width)] = new Color(0, 0, 0, 1);
			pixels[UvToIndex(s + Vector2.up / _width - Vector2.right / _width)] = new Color(0, 0, 0, 1);
			pixels[UvToIndex(s - Vector2.up / _width - Vector2.right / _width)] = new Color(0, 0, 0, 1);
			pixels[UvToIndex(s - Vector2.up / _width + Vector2.right / _width)] = new Color(0, 0, 0, 1);
		}
		_starTex.SetPixels(pixels);
		_starTex.Apply();
	}
}