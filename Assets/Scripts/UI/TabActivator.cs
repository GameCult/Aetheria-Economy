/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabActivator : MonoBehaviour
{
    public TabGroup TabGroup;
    public TabButton TabButton;
    
    void Start()
    {
        TabGroup.OnTabChange += button => gameObject.SetActive(button == TabButton);
        gameObject.SetActive(false);
    }
}
