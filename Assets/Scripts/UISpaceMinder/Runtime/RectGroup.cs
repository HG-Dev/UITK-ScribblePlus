using System;
using System.Collections.Generic;
using System.Linq;
using UISpaceMinder.Shims;
using UnityEngine;

namespace UISpaceMinder
{
    /// <summary>
    /// Combines a Rect instance with a string for additional context information.
    /// </summary>
    public readonly struct NamedRect : IEquatable<NamedRect>
    {
        public readonly Rect rect;
        public readonly string name;

        public NamedRect(Rect rect, string name = null)
        {
            this.rect = rect;
            this.name = string.IsNullOrEmpty(name) ? rect.center.ToString() : name;
        }

        public bool Equals(NamedRect other)
        {
            return rect.Equals(other.rect) && name.Equals(other.name);
        }

        public override string ToString()
        {
            return $"NamedRect <{name}>: {rect}";
        }

        public static IEnumerable<Rect> EnumerateRects(IEnumerable<NamedRect> namedRects) => namedRects.Select(named => named.rect);
    }

    /// <summary>
    /// A named collection of NamedRect that define where UI is present, or alternatively, absent.
    /// </summary>
    public readonly struct NamedRectGroup : IEquatable<NamedRectGroup>
    {
        public readonly Rect bounds;
        public readonly NamedRect[] collection;

        public IEnumerable<Rect> Rects => collection.Select(pair => pair.rect);

        public NamedRectGroup(Rect bounds, NamedRect[] collection)
        {
            this.bounds = bounds;
            this.collection = collection;
        }

        public NamedRectGroup(Rect bounds, IEnumerable<NamedRect> collection)
        {
            this.bounds = bounds;
            this.collection = collection.ToArray();
        }

        public NamedRectGroup(IEnumerable<NamedRect> collection)
        {
            this.collection = collection.ToArray();
            this.bounds = this.collection.Select(entry => entry.rect).Encapsulate();
        }

        public static NamedRectGroup Empty => new(default, Array.Empty<NamedRect>());

        public override string ToString()
        {
            return $"RectGroup: Bounds = {bounds}\n{collection.Length} in collection";
        }

        public bool Equals(NamedRectGroup other)
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
}
