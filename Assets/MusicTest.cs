using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class MusicTest : MonoBehaviour
{
    public float TransitionTime;
    public AudioMixerSnapshot CombatSnapshot;
    public AudioMixerSnapshot AmbientSnapshot;

    void Update()
    {
        if(Keyboard.current.aKey.wasPressedThisFrame)
            AmbientSnapshot.TransitionTo(TransitionTime);
        if(Keyboard.current.cKey.wasPressedThisFrame)
            CombatSnapshot.TransitionTo(TransitionTime);
    }
}
