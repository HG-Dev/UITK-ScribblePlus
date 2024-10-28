using System;
using System.Linq;
using UISpaceMinder.Shims;
using UnityEngine;

namespace UISpaceMinder
{
    /// <summary>
    /// A collection of bounding rectangles that define where UI is present, or alternatively, absent.
    /// </summary>
    public readonly struct RectGroup : IEquatable<RectGroup>
    {
        public readonly Rect bounds;
        public readonly Rect[] collection;

        public RectGroup(Rect bounds, Rect[] collection)
        {
            this.bounds = bounds;
            this.collection = collection;
        }

        public RectGroup(Rect[] collection)
        {
            this.bounds = collection.Encapsulate();
            this.collection = collection;
        }

        public static RectGroup Empty => new RectGroup(default, Array.Empty<Rect>());

        public override string ToString()
        {
            return $"RectGroup: Bounds = {bounds}\n{collection.Length} in collection";
        }

        public bool Equals(RectGroup other)
        {
            if (collection.Length != other.collection.Length
                || !bounds.Equals(other.bounds)) 
                return false;

            for (int i = 0; i < collection.Length; i++)
                if (!collection[i].Equals(other.collection[i]))
                    return false;

            return true;
        }
    }

    /// <summary>
    /// A collection of bounding rectangles that define where UI is present, or alternatively, absent.
    /// </summary>
    //public readonly struct RectSpace : IEquatable<RectGroup>
    //{
    //    public readonly Rect limits;
    //    public readonly Rect normalized;
    //    public Rect bounds => group.bounds;
    //    public Rect[] collection => group.collection;

    //    private readonly RectGroup group;

    //    public RectSpace(Rect limits, Rect bounds, Rect[] collection)
    //    {
    //        this.limits = limits;
    //        this.normalized = bounds.Normalize(limits);
    //        this.group = new RectGroup(bounds, collection);
    //    }

    //    public RectSpace(Rect limits, Rect[] collection)
    //    {
    //        this.limits = limits;
    //        group = new RectGroup(collection);
    //        this.normalized = group.bounds.Normalize(limits);
    //    }

    //    public static RectGroup Empty => new RectGroup(default, Array.Empty<Rect>());

    //    public override string ToString()
    //    {
    //        return $"RectGroup: Bounds = {bounds}\n{collection.Length} in collection";
    //    }

    //    public bool Equals(RectGroup other)
    //    {
    //        if (collection.Length != other.collection.Length
    //            || !bounds.Equals(other.bounds))
    //            return false;

    //        for (int i = 0; i < collection.Length; i++)
    //            if (!collection[i].Equals(other.collection[i]))
    //                return false;

    //        return true;
    //    }
    //}
}
