/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ZoneObject : MonoBehaviour
{
    public MeshRenderer Icon;

    public TextMeshPro Label;

    public TextMeshPro Message;
    
    [HideInInspector]
    public List<MeshRenderer> Children = new List<MeshRenderer>();
}
