using System;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

/// <summary>
/// Lets several scripts listen to the same ROS topic and stop listening independently.
/// ROSConnection.Unsubscribe(topic) removes every callback on that topic, so if two scripts
/// subscribed directly, the first one to clean up would silently cut off the other.
/// Here the topic is subscribed once, and unsubscribed when the last listener is removed.
/// </summary>
public static class SharedRosSubscription
{
    private class Entry
    {
        public ROSConnection Ros;
        public readonly List<Delegate> Handlers = new List<Delegate>();
    }

    private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

    /// <summary>Adds a listener for <paramref name="topic"/>.</summary>
    public static void Add<T>(ROSConnection ros, string topic, Action<T> handler) where T : Message
    {
        if (ros == null || string.IsNullOrEmpty(topic) || handler == null) return;

        // A new ROSConnection (for example after reloading the scene) needs its own subscription.
        if (!entries.TryGetValue(topic, out Entry entry) || entry.Ros != ros)
        {
            entry = new Entry { Ros = ros };
            entries[topic] = entry;
            ros.Subscribe<T>(topic, msg => Dispatch(topic, msg));
        }
        entry.Handlers.Add(handler);
    }

    /// <summary>Removes a listener added with <see cref="Add{T}"/>.</summary>
    public static void Remove<T>(string topic, Action<T> handler) where T : Message
    {
        if (string.IsNullOrEmpty(topic) || !entries.TryGetValue(topic, out Entry entry)) return;

        entry.Handlers.Remove(handler);
        if (entry.Handlers.Count == 0)
        {
            entries.Remove(topic);
            if (entry.Ros != null) entry.Ros.Unsubscribe(topic);
        }
    }

    private static void Dispatch<T>(string topic, T msg) where T : Message
    {
        if (!entries.TryGetValue(topic, out Entry entry)) return;
        // Copy so a handler may remove itself while being called.
        foreach (Delegate d in entry.Handlers.ToArray())
        {
            ((Action<T>)d)(msg);
        }
    }
}
