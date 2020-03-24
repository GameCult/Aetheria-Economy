using System;
using System.Collections;
using System.Collections.Generic;

public abstract class NotAnActionCollection{}

public class ActionCollection<T> : NotAnActionCollection where T : Message
{
    private List<Action<T>> _actions = new List<Action<T>>();

    public void Add(Action<T> action)
    {
        _actions.Add(action);
    }

    public void Invoke(T message)
    {
        foreach (var action in _actions)
        {
            action(message);
        }
    }

    public void Clear()
    {
        _actions.Clear();
    }
}