using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityHelpers
{
    public static T LoadAsset<T>(string path) where T : Object
    {
        return Resources.Load<T>(path.Substring("Assets/Resources/".Length).Split('.')[0]);
    }
}
