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
	private Texture2D _preview;
	private int _width = 0;
	private TextAsset _nameData;
	private bool _showNameData;
	private bool _showCultureMaps;
	private Dictionary<string,bool> _expandRegion = new Dictionary<string, bool>();
	private Dictionary<string,bool> _expandSubregion = new Dictionary<string, bool>();
	private NameData _names = new NameData();
	private GalaxyMapLayerData _currentLayer;
	private string _currentLayerName = "Star Density";
	private bool _showStarEditor;
	private int _hilbertOrder = 6;
	private ulong _hilbertIndex;
	private int _starCount = 1;
	private int _nameMinLength = 5;
	private int _nameMaxLength = 8;
	private float _maxLinkLength = .1f;
	private float _linkFilter = .5f;
	private IEnumerable<Vector2> _stars = new Vector2[0];
	private bool _drawStars;
	private bool _drawCulture;
	private bool _drawLinks;
	private VoronoiMesh< Vertex2, Cell2, VoronoiEdge<Vertex2, Cell2>> _voronoiMesh;
	private List<VoronoiLink> _starLinks = new List<VoronoiLink>();

	public override void OnInspectorGUI()
	{
		var galaxy = target as Galaxy;

		if (_currentLayer == null)
			_currentLayer = galaxy.MapData.StarDensity;
		
		if (Screen.width != _width)
		{
			_width = Screen.width;
			_preview = new Texture2D(_width,_width,TextureFormat.ARGB32,false);
			Render(galaxy.MapData, _currentLayer);
		}
		
		GUILayout.Label("Preview", EditorStyles.boldLabel);
		EditorGUI.DrawPreviewTexture(EditorGUILayout.GetControlRect(false, _width), _preview);
		EditorGUI.BeginChangeCheck();
		_drawStars = EditorGUILayout.ToggleLeft($"Display {_stars.Count()} Stars", _drawStars);
		_drawCulture = EditorGUILayout.ToggleLeft("Display Cultures", _drawCulture);
		_drawLinks = EditorGUILayout.ToggleLeft($"Display {_starLinks.Count} Links", _drawLinks);
		GUILayout.Space(10);
		
		// Show default inspector property editor
		DrawDefaultInspector ();

		if (galaxy.MapData.CultureDensities.Count != galaxy.NameDatabase.Count)
			galaxy.MapData.CultureDensities = galaxy.NameDatabase.Select(continent=>new GalaxyMapLayerData()).ToList();
		EditorGUILayout.BeginVertical("Box");
		EditorGUI.indentLevel++;
		GUILayout.Label(_currentLayerName);
		Inspect(_currentLayer);
		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
		if(EditorGUI.EndChangeCheck())
			Render(galaxy.MapData, _currentLayer);
		
		EditorGUILayout.BeginVertical("Box");
		EditorGUI.indentLevel++;
		_showCultureMaps = EditorGUILayout.Foldout(_showCultureMaps, "Culture Density Maps");
		if (_showCultureMaps)
		{
			EditorGUI.indentLevel++;
			for (var i = 0; i < galaxy.MapData.CultureDensities.Count; i++)
			{
				var cultureMap = galaxy.MapData.CultureDensities[i];
				var data = galaxy.NameDatabase[i];
				if (GUILayout.Button("Inspect " + data.Name))
				{
					_currentLayerName = data.Name;
					_currentLayer = cultureMap;
					Render(galaxy.MapData, _currentLayer);
				}
			}
			EditorGUI.indentLevel--;
		}
		else if(_currentLayer != galaxy.MapData.StarDensity)
		{
			_currentLayerName = "Star Density";
			_currentLayer = galaxy.MapData.StarDensity;
			Render(galaxy.MapData, _currentLayer);
		}
		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
		
		EditorGUILayout.BeginVertical("Box");
		EditorGUI.indentLevel++;
		_showNameData = EditorGUILayout.Foldout(_showNameData, "Name Database Tools");
		if (_showNameData)
		{
			EditorGUI.indentLevel++;
			foreach (var region in _names.Names.Keys)
			{
				if (!_expandRegion.ContainsKey(region))
					_expandRegion[region] = false;
				_expandRegion[region] = EditorGUILayout.Foldout(_expandRegion[region], region + ": " + _names.Names[region].Count + " Subregions");
				if (_expandRegion[region])
				{
					EditorGUI.indentLevel++;
					foreach (var subregion in _names.Names[region].Keys)
					{
						if (!_expandSubregion.ContainsKey(subregion))
							_expandSubregion[subregion] = false;
						_expandSubregion[subregion] = EditorGUILayout.Foldout(_expandSubregion[subregion], subregion + ": " + _names.Names[region][subregion].Count + " Towns");
						if (_expandSubregion[subregion])
						{
							EditorGUI.indentLevel++;
							foreach (var town in _names.Names[region][subregion])
							{
								GUILayout.Label(town);
							}
							EditorGUI.indentLevel--;
						}
					}
					EditorGUI.indentLevel--;
				}
			}
			EditorGUI.indentLevel--;
			GUILayout.Space(10);
			_nameData = EditorGUILayout.ObjectField(_nameData, typeof(TextAsset), false) as TextAsset;
			if (GUILayout.Button("Parse Data"))
			{
				_names.Names.Clear();
				foreach (var s in _nameData.text.Split('\n'))
					_names.Add(s);
			}
			if (GUILayout.Button("Filter Data"))
			{
				if (!_names.Names.ContainsKey("World"))
				{
					_names.Names["World"] = new Dictionary<string, List<string>>();
					_names.Names["World"]["Miscellaneous"] = new List<string>();
				}
				foreach (var region in _names.Names.Keys.Where(s=>s!="World").ToArray())
				{
					if (!_names.Names[region].ContainsKey("Miscellaneous"))
						_names.Names[region]["Miscellaneous"] = new List<string>();
					foreach (var subregion in _names.Names[region].Keys.Where(s=>s!="Miscellaneous").ToArray())
					{
						if (_names.Names[region][subregion].Count < 500)
						{
							_names.Names[region]["Miscellaneous"].AddRange(_names.Names[region][subregion]);
							_names.Names[region].Remove(subregion);
						}
					}
					if (_names.Names[region].Count == 1)
					{
						_names.Names["World"]["Miscellaneous"].AddRange(_names.Names[region]["Miscellaneous"]);
						_names.Names.Remove(region);
					}
				}
			}
			if (GUILayout.Button("Set Data"))
			{
				var data = new List<ContinentData>();
				data.AddRange(_names.Names.Select(kvp => new ContinentData{Name = kvp.Key,Regions = new List<RegionData>()}));
				data.ForEach(c=>c.Regions.AddRange(_names.Names[c.Name].Select(kvp=> new RegionData{Name = kvp.Key,Towns = kvp.Value})));
				galaxy.NameDatabase = data;
			}
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
			}
			if (_stars.Any())
			{
				_nameMinLength = EditorGUILayout.IntField("Minimum Length", _nameMinLength);
				_nameMaxLength = EditorGUILayout.IntField("Maximum Length", _nameMaxLength);
				if (GUILayout.Button("Generate Star Names"))
				{
					galaxy.MapData.Stars = _stars.Select(s =>
					{
						var culture = galaxy.MapData.CultureDensities.IndexOf(galaxy.MapData.CultureDensities.OrderByDescending(c => c.Evaluate(s, galaxy.MapData))
							.First());
						var subculture = UnityEngine.Random.Range(0, galaxy.NameDatabase[culture].Regions.Count);
						var markov = new MarkovNameGenerator(galaxy.NameDatabase[culture].Regions[subculture].Towns, 2, _nameMinLength, _nameMaxLength);
						return new StarData
						{
							Position = s,
							NameSource = (galaxy.NameDatabase[culture].Regions[subculture].Name == "Miscellaneous")
								? galaxy.NameDatabase[culture].Name
								: galaxy.NameDatabase[culture].Regions[subculture].Name,
							Names = Enumerable.Range(0,20).Select(_ => markov.NextName).ToList()
						};
					}).ToList();
				}
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
					}
					if (GUILayout.Button("Apply Star Links"))
					{
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

/*
		EditorGUI.BeginChangeCheck();
		_arms = EditorGUILayout.IntField("Arms", _arms);
		_twist = EditorGUILayout.FloatField("Twist", _twist);
		_twistPower = EditorGUILayout.FloatField("Twist Power", _twistPower);
		EditorGUILayout.field
		
		_edgeReduction = EditorGUILayout.FloatField("Edge Reduction", _edgeReduction);
		_coreBoost = EditorGUILayout.FloatField("Core Boost", _coreBoost);
		_coreBoostOffset = EditorGUILayout.FloatField("Core Boost Offset", _coreBoostOffset);
		_coreBoostPower = EditorGUILayout.FloatField("Core Boost Power", _coreBoostPower);
		_noiseAmplitude = EditorGUILayout.FloatField("Noise Amplitude", _noiseAmplitude);
		_noise.SetFractalGain(_noiseGain = EditorGUILayout.FloatField("Noise Gain", _noiseGain));
		_noise.SetFractalLacunarity(_noiseLacunarity = EditorGUILayout.FloatField("Noise Lacunarity", _noiseLacunarity));
		_noise.SetFractalOctaves(_noiseOctaves = EditorGUILayout.IntField("Noise Octaves", _noiseOctaves));
		_noise.SetFrequency(_noiseFrequency = EditorGUILayout.FloatField("Noise Frequency", _noiseFrequency));
		if(EditorGUI.EndChangeCheck())
			Render();
		if (GUILayout.Button("Randomize Seed"))
		{
			_noise.SetSeed((int) (UnityEngine.Random.value*int.MaxValue));
			Render();
		}
		if(GUILayout.Button("Regenerate"))
			Render();
		*/
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
		layer.NoiseOffset = EditorGUILayout.FloatField("Noise Seed", layer.NoiseOffset);
		layer.SpokeOffset = EditorGUILayout.FloatField("Spoke Offset", layer.SpokeOffset);
		layer.SpokeScale = EditorGUILayout.FloatField("Spoke Scale", layer.SpokeScale);
	}

	void Render(GalaxyMapData galaxy, GalaxyMapLayerData layer)
	{
		Color[] pixels = new Color[_width*_width];
		Func<int, Vector2> indexToUV = i => new Vector2(i % _width / (float) _width, i / _width / (float) _width);
		Func<Vector2, int> uvToIndex = v => (int) (v.y * _width) * _width + (int) (v.x * _width);
		for (var i = 0; i < pixels.Length; i++)
		{
			Vector2 uv = indexToUV(i);
			pixels[i] = layer.Evaluate(uv, galaxy) * Color.white;
			if (_drawCulture)
				pixels[i] += Color.HSVToRGB(
					(float) galaxy.CultureDensities.IndexOf(galaxy.CultureDensities.OrderByDescending(m => m.Evaluate(uv, galaxy)).First()) /
					galaxy.CultureDensities.Count,
					1,
					.5f);
		}
		if(_drawStars)
			foreach (var s in _stars)
			{
				pixels[uvToIndex(s)] = Color.white;
				pixels[uvToIndex(s + Vector2.up / _width)] += Color.white / 2;
				pixels[uvToIndex(s - Vector2.up / _width)] += Color.white / 2;
				pixels[uvToIndex(s + Vector2.right / _width)] += Color.white / 2;
				pixels[uvToIndex(s - Vector2.right / _width)] += Color.white / 2;
			}
		if(_drawLinks)
			foreach (var l in _starLinks)
			{
				var pixelCount = Mathf.Max((int) (Mathf.Abs((float) (l.point1.x - l.point2.x)) * _width), (int) (Mathf.Abs((float) (l.point1.y - l.point2.y)) * _width));
				//Debug.Log($"Marching across {pixelCount} pixels!");
				for(int i=0;i<pixelCount;i++)
					pixels[uvToIndex(Vector2.Lerp(l.point1.ToVector2(),l.point2.ToVector2(),(float)i/pixelCount))] += Color.white / 2;
			}
		_preview.SetPixels(pixels);
		_preview.Apply();
	}
}