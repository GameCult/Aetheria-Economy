using System.Collections;
using System.Collections.Generic;

// Thanks, https://stackoverflow.com/a/49924960
public sealed class SizedQueue<T> : Queue<T>
{
    public int FixedCapacity { get; }
    
    public SizedQueue(int fixedCapacity)
    {
        FixedCapacity = fixedCapacity;
    }

    /// <summary>
    /// If the total number of item exceed the capacity, the oldest ones automatically dequeues.
    /// </summary>
    /// <returns>The dequeued value, if any.</returns>
    public new T Enqueue(T item)
    {
        base.Enqueue(item);
        if (Count > FixedCapacity)
        {
            return Dequeue();
        }
        return default;
    }
}