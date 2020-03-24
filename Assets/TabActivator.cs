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
