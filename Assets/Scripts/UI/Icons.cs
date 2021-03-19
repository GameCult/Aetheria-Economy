/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Icons", menuName = "Aetheria/Icons")]
public class Icons : ScriptableObject
{
    public Texture2D plus;
    public Texture2D minus;
    
    private const string AssetPath = "Icons";
    private static Icons _instance;
    public static Icons Instance {
        get { return _instance ?? (_instance = Resources.Load<Icons>(AssetPath)); }
    }
}
