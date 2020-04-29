using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AgentTask
{
    public abstract AgentJob JobType { get; }
    public int Priority;
    public Guid Zone;
}

public enum AgentJob
{
    Mine,
    Haul,
    Tow,
    Defend,
    Attack,
    Explore
}
