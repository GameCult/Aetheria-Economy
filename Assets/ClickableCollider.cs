using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickableCollider : MonoBehaviour
{
    public event Action<ClickableCollider> OnClick;
}
