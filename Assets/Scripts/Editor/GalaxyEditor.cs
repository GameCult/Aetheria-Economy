using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;
using Random = UnityEngine.Random;
using static Unity.Mathematics.math;

[CustomEditor(typeof(Galaxy))]
public class GalaxyEditor : Editor
{
	private static readonly RethinkDB R = RethinkDB.R;
	
	private int _width;
	private bool _showResourceMaps;
	private GalaxyMapLayerData _currentLayer;
	private string _currentLayerName = "Star Density";
	private bool _showStarEditor;
	private int _hilbertOrder = 6;
	private ulong _hilbertIndex;
	private int _starCount = 1;
	private float _minStarDistance = .01f;
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
	private Connection _connection;

	private void OnEnable()
	{
		_white = Color.white.ToTexture();
		_galaxyMat = new Material(Shader.Find("Unlit/GalaxyMap"));
		_transparent = new Material(Shader.Find("Unlit/Transparent"));
		_width = Screen.width;
		_starTex = new Texture2D(_width,_width, TextureFormat.ARGB32, false);
		_linkTex = new Texture2D(_width,_width, TextureFormat.ARGB32, false);
		// var galaxy = target as Galaxy;
		// _stars = galaxy.MapData.Stars.Select(s => s.Position);
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
		_galaxyMat.SetFloat("Arms", galaxy.MapData.GlobalData.Arms);
		_galaxyMat.SetFloat("Twist", galaxy.MapData.GlobalData.Twist);
		_galaxyMat.SetFloat("TwistPower", galaxy.MapData.GlobalData.TwistPower);
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
		var rect = GetControlRect(false, _width);
		EditorGUI.DrawPreviewTexture(rect, _white, _galaxyMat);
		if(_drawLinks)
			EditorGUI.DrawPreviewTexture(rect, _linkTex, _transparent);
		if(_drawStars)
			EditorGUI.DrawPreviewTexture(rect, _starTex, _transparent);
		
		_drawStars = ToggleLeft($"Display {_stars.Count()} Stars", _drawStars);
		_drawResource = ToggleLeft("Display Resources", _drawResource);
		_drawLinks = ToggleLeft($"Display {_starLinks.Count} Links", _drawLinks);
		
		GUILayout.Space(10);
		
		// Show default inspector property editor
		DrawDefaultInspector ();

		BeginVertical("Box");
		EditorGUI.indentLevel++;
		GUILayout.Label(_currentLayerName);
		Inspect(_currentLayer);
		EditorGUI.indentLevel--;
		EndVertical();
		
		BeginVertical("Box");
		EditorGUI.indentLevel++;
		_showResourceMaps = Foldout(_showResourceMaps, "Resource Density Maps");
		if (_showResourceMaps)
		{
			EditorGUI.indentLevel++;
			foreach (var resourceDensity in galaxy.MapData.ResourceDensities.ToArray())
			{
				BeginHorizontal();
				var newName = DelayedTextField(resourceDensity.Key);
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
				EndHorizontal();
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
		EndVertical();
		
		BeginVertical("Box");
		EditorGUI.indentLevel++;
		_showStarEditor = Foldout(_showStarEditor, "Star Tools");
		if (_showStarEditor)
		{
			_hilbertOrder = IntField("Hilbert Order", _hilbertOrder);
			_starCount = IntField("Star Count", _starCount);
			//_hilbertIndex = (ulong) EditorGUILayout.IntField("Hilbert Index", (int) _hilbertIndex);
			if (GUILayout.Button("Evaluate Hilbert Curve"))
			{
				var points = EvaluateHilbert(_hilbertOrder,false);
				Debug.Log($"Hilbert curve has {points.Count()} points, resolution {Mathf.RoundToInt(points.Max(p=>p.x))+1}");
			}

			_minStarDistance = FloatField("Minimum Star Distance", _minStarDistance);
			
			if (GUILayout.Button("Generate Stars"))
			{
				var points = EvaluateHilbert(_hilbertOrder).ToArray();
				var stars = new List<Vector2>();
				int bail = 0;
				while (stars.Count < _starCount && bail < 10)
				{
					var accum = 0f;
					foreach (var hp in points.Select(p=>p+Random.insideUnitCircle * ((points[0]-points[1]).magnitude/2)))
					{
						var den = galaxy.MapData.StarDensity.Evaluate(hp, galaxy.MapData.GlobalData);
						if(!float.IsNaN(den))
							accum += saturate(den) * Random.value;
//						else
//							Debug.Log($"Density at ({hp.x},{hp.y}) is NaN");
						if (accum > 1 && (!stars.Any() || stars.Min(s=>(s-hp).magnitude) > _minStarDistance) )
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
				_maxLinkLength = FloatField("Max Link Length", _maxLinkLength);
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
					_linkFilter = FloatField("Link Filter Percentage", _linkFilter);
					if (GUILayout.Button("Filter Star Links"))
					{
						var bail = 0;
						var count = _starLinks.Count * Mathf.Clamp01(_linkFilter);
						var deadLinks = new List<VoronoiLink>();
						for (int i = 0; i < count && bail < count*10; bail++)
						{
							var link = _starLinks.ElementAt(Random.Range(0, _starLinks.Count));
							
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
					
					if (GUILayout.Button("Connect to RethinkDB"))
						_connection = R.Connection().Hostname(EditorPrefs.GetString("RethinkDB.URL")).Port(RethinkDBConstants.DefaultPort).Timeout(60).Connect();
					EditorGUI.BeginDisabledGroup(_connection == null);

					if (GUILayout.Button("Drop Galaxy Table"))
						R.Db("Aetheria").TableDrop("Galaxy").Run(_connection);

					if (GUILayout.Button("Create Galaxy Table"))
						R.Db("Aetheria").TableCreate("Galaxy").Run(_connection);
					
					EditorGUI.BeginDisabledGroup(!galaxy.MapData.Stars.Any());
					
					if (GUILayout.Button("Upload Star Data"))
					{
						Converter.Serializer.Converters.Add(new MathJsonConverter());
						JsonConvert.DefaultSettings = () => new JsonSerializerSettings
						{
							Converters = new List<JsonConverter>
							{
								new MathJsonConverter(),
								Converter.DateTimeConverter,
								Converter.BinaryConverter,
								Converter.GroupingConverter,
								Converter.PocoExprConverter
							}
						};
						var starIDs = new Dictionary<StarData, Guid>();
						foreach (var star in galaxy.MapData.Stars)
							starIDs[star] = Guid.NewGuid();
						foreach (var star in galaxy.MapData.Stars)
						{
							R.Db("Aetheria").Table("Galaxy").Insert(new ZoneData
							{
								ID = starIDs[star],
								Name = starIDs[star].ToString().Substring(0, 8),
								Wormholes = star.Links.Select(i => starIDs[galaxy.MapData.Stars[i]]).ToList()
							}).Run(_connection);
						}

						galaxy.MapData.GlobalData.MapLayers["StarDensity"] = galaxy.MapData.StarDensity.ID = Guid.NewGuid();
						R.Db("Aetheria").Table("Galaxy").Insert(galaxy.MapData.StarDensity).Run(_connection);
						foreach (var kvp in galaxy.MapData.ResourceDensities)
						{
							galaxy.MapData.GlobalData.MapLayers[kvp.Key] = kvp.Value.ID = Guid.NewGuid();
							R.Db("Aetheria").Table("Galaxy").Insert(kvp.Value).Run(_connection);
						}
						
						R.Db("Aetheria").Table("Galaxy").Insert(galaxy.MapData.GlobalData).Run(_connection);
					}
					
					EditorGUI.EndDisabledGroup();
					
					EditorGUI.EndDisabledGroup();
				}
			}
		}
		EditorGUI.indentLevel--;
		EndVertical();
		
		
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
			SpaceFillingCurve.ReverseHilbert(order, i, out x, out y);
			points.Add(new Vector2(x, y));
			if (x > max)
				max = x;
			i++;
		}
		return normalized ? points.Select(p => p / (max + 1)) : points;
	}

	void Inspect(GalaxyMapLayerData layer)
	{
		layer.EdgeReduction = FloatField("Edge Reduction", layer.EdgeReduction);
		layer.CoreBoost = FloatField("Core Boost", layer.CoreBoost);
		layer.CoreBoostOffset = FloatField("Core Boost Offset", layer.CoreBoostOffset);
		layer.CoreBoostPower = FloatField("Core Boost Power", layer.CoreBoostPower);
		layer.NoisePosition = FloatField("Noise Position", layer.NoisePosition);
		layer.NoiseOffset = FloatField("Noise Offset", layer.NoiseOffset);
		layer.NoiseAmplitude = FloatField("Noise Amplitude", layer.NoiseAmplitude);
		layer.NoiseGain = FloatField("Noise Gain", layer.NoiseGain);
		layer.NoiseLacunarity = FloatField("Noise Lacunarity", layer.NoiseLacunarity);
		layer.NoiseOctaves = IntField("Noise Octaves", layer.NoiseOctaves);
		layer.NoiseFrequency = FloatField("Noise Frequency", layer.NoiseFrequency);
		layer.SpokeOffset = FloatField("Spoke Offset", layer.SpokeOffset);
		layer.SpokeScale = FloatField("Spoke Scale", layer.SpokeScale);
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
			var pixelCount = Mathf.Max((int) (Mathf.Abs(l.point1.x - l.point2.x) * _width), (int) (Mathf.Abs(l.point1.y - l.point2.y) * _width));
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