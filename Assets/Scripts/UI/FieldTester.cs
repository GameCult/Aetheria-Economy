using System;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class FieldTester : MonoBehaviour
{
    public Camera Camera;
    public FieldDriver TestField;
    public PropertiesPanel Properties;
    
    private AetheriaInput _input;
    private bool _forceThrust;

    private void Start()
    {
        _input = new AetheriaInput();
        _input.Player.Enable();
        Properties.AddField("FOV", () => Camera.fieldOfView, f => Camera.fieldOfView = f, 15, 45);
        Properties.AddButton("Melee", () => TestField.Melee());
        Properties.AddField("Force Thrust", () => _forceThrust, b => _forceThrust = b);
        Properties.Inspect(TestField, true, true);
        Properties.AddProperty("Current Hits", () => TestField.HitCount.ToString());
    }

    private void Update()
    {
        TestField.Throttle = _forceThrust ? float2(0,1) : _input.Player.Move.ReadValue<Vector2>();
    }
}