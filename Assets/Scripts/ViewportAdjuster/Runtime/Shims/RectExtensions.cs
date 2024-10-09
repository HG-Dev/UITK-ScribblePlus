using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ViewportAdjuster.Shims
{
    [Flags]
    public enum RectSides : byte
    {
        None = 0,
        YMax = 1 << 0,   // 1
        XMax = 1 << 1,   // 2
        YMin = 1 << 2,   // 4
        XMin = 1 << 3,   // 8
        All = YMax | XMax | YMin | XMin
    }
    
    public readonly struct RectSizeComparer : IEqualityComparer<Rect>, IComparer<Rect>
    {
        private readonly Vector2 _mask;

        public bool Equals(Rect a, Rect b)
        {
            return a.size.Equals(b.size);
        }

        public int GetHashCode(Rect obj)
        {
            return obj.size.GetHashCode();
        }

        public RectSizeComparer(bool useX = true, bool useY = true)
        {
            _mask = new Vector2(useX ? 1 : 0, useY ? 1 : 0);
        }

        public int Compare(Rect a, Rect b)
        {
            var aSize = a.size;
            var bSize = b.size;
            aSize.Scale(_mask);
            bSize.Scale(_mask);
            return aSize.sqrMagnitude.CompareTo(bSize.sqrMagnitude);
        }
    }
    
    public static class RectExtensions
    {
        public static bool OverlapsVertically(this Rect self, Rect other)
        {
            var selfCentered = new Rect(self) { center = new Vector2(0, self.center.y) };
            var otherCentered = new Rect(other) { center = new Vector2(0, other.center.y) };
            return selfCentered.Overlaps(otherCentered);
        }
        
        public static bool OverlapsHorizontally(this Rect self, Rect other)
        {
            var selfCentered = new Rect(self) { center = new Vector2(self.center.x, 0) };
            var otherCentered = new Rect(other) { center = new Vector2(other.center.x, 0) };
            return selfCentered.Overlaps(otherCentered);
        }

        private static bool AnyAxisOverlap(Rect a, Rect b, out (bool horizontal, bool vertical) result)
        {
            result = (a.OverlapsHorizontally(b), a.OverlapsVertically(b));
            return result.horizontal || result.vertical;
        }
            
        
        /// <summary>
        /// Test to confirm whether this (self) rect overlaps with 'other',
        /// and which sides of other it overlaps with.
        /// Equivalent rects will return all sides as flagged.
        /// </summary>
        /// <returns>
        /// Whether or not this rect touches 'other', and if so,
        /// which side(s) on other this rect touches.
        /// </returns>
        public static bool OverlapsOrTouches(this Rect self, Rect other, out RectSides sides)
        {
            sides = RectSides.None;

            if (!AnyAxisOverlap(self, other, out (bool horizontal, bool vertical) overlap))
                return false;
            
            if (TouchesSideApproximately(self, other, RectSides.XMin, overlap) 
                 || (overlap.vertical && self.xMin <= other.xMin && self.xMax > other.xMin))
                sides |= RectSides.XMin;
            if (TouchesSideApproximately(self, other, RectSides.YMin, overlap) 
                 || (overlap.horizontal && self.yMin <= other.yMin && self.yMax > other.yMin))
                sides |= RectSides.YMin;
            if (TouchesSideApproximately(self, other, RectSides.XMax, overlap) 
                 || (overlap.vertical && self.xMax >= other.xMax && self.xMin < other.xMax))
                sides |= RectSides.XMax;
            if (TouchesSideApproximately(self, other, RectSides.YMax, overlap) 
                 || (overlap.horizontal && self.yMax >= other.yMax && self.yMin < other.yMax))
                sides |= RectSides.YMax;

            return sides > RectSides.None;
        }

        public static bool TouchesSideApproximately(this Rect self, Rect other, RectSides sides) =>
            AnyAxisOverlap(self, other, out (bool horizontal, bool vertical) overlap) 
            && TouchesSideApproximately(self, other, sides, overlap);

        private static bool TouchesSideApproximately(Rect alpha, Rect beta, RectSides sides, (bool horizontal, bool vertical) overlap)
        {
            if (sides is RectSides.None)
                return !alpha.Overlaps(beta);
            
            var tests = Enum.GetValues(typeof(RectSides))
                .Cast<RectSides>()
                .Where(value => sides.HasFlag(value));
            
            foreach (var test in tests)
            {
                switch (test)
                {
                    // First test external adjacency, then internal
                    case RectSides.None:
                        break;
                    case RectSides.YMin:
                        if (!overlap.horizontal)
                            return false;
                        if (Mathf.Approximately(alpha.yMax, beta.yMin)
                            || Mathf.Approximately(alpha.yMin, beta.yMin))
                            break;
                        return false;
                    case RectSides.XMax:
                        if (!overlap.vertical)
                            return false;
                        if (Mathf.Approximately(alpha.xMin, beta.xMax)
                            || Mathf.Approximately(alpha.xMax, beta.xMax))
                            break;
                        return false;
                    case RectSides.YMax:
                        if (!overlap.horizontal)
                            return false;
                        if (Mathf.Approximately(alpha.yMin, beta.yMax)
                            || Mathf.Approximately(alpha.yMax, beta.yMax))
                            break;
                        return false;
                    case RectSides.XMin:
                        if (!overlap.vertical)
                            return false;
                        if (Mathf.Approximately(alpha.xMax, beta.xMin)
                            || Mathf.Approximately(alpha.xMin, beta.xMin))
                            break;
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            return true;
        }

        /// <summary>
        /// Confirms whether self touches the entire length of one or more sides on other,
        /// given by the sides parameter.
        /// </summary>
        public static bool OverlapsOrTouchesEntirely(this Rect self, Rect other, out RectSides sides)
        {
            sides = RectSides.None;

            var fullHorizontalOverlap = self.xMin <= other.xMin && self.xMax >= other.xMax;
            var fullVerticalOverlap = self.yMin <= other.yMin && self.yMax >= other.yMax;
            
            if (fullVerticalOverlap &&
                (self.TouchesSideApproximately(other, RectSides.XMin)
                 || (self.xMin <= other.xMin && self.xMax >= other.xMin)))
                sides |= RectSides.XMin;
            if (fullHorizontalOverlap &&
                (self.TouchesSideApproximately(other, RectSides.YMax) 
                 || (self.yMin <= other.yMin && self.yMax >= other.yMin)))
                sides |= RectSides.YMax;
            if (fullVerticalOverlap &&
                (self.TouchesSideApproximately(other, RectSides.XMax) 
                 || (self.xMax >= other.xMax && self.xMin <= other.xMax)))
                sides |= RectSides.XMax;
            if (fullHorizontalOverlap &&
                (self.TouchesSideApproximately(other, RectSides.YMin) 
                 || (self.yMax >= other.yMax && self.yMin <= other.yMax)))
                sides |= RectSides.YMin;

            return sides > RectSides.None;
        }

        /// <summary>
        /// Beginning from an initial seed rect,
        /// sort rects by distance and encapsulate all that overlap.
        /// Use an R-tree in the future?
        /// </summary>
        /// <param name="rects"></param>
        /// <returns>A list of Rect, including start, that are adjacent to each other</returns>
        public static List<Rect> AllAdjacent(this Rect start, IEnumerable<Rect> others)
        {
            var unconfirmed = new List<Rect>(others
                .OrderBy(other => Vector2.Distance(start.center, other.center)));
            var confirmed = new List<Rect>{start};

            while (unconfirmed.Count > 0)
            {
                var next = unconfirmed.FirstOrDefault(u => confirmed.Any(c => u.OverlapsOrTouches(c, out _)));
                if (next != default)
                {
                    confirmed.Add(next);
                    unconfirmed.Remove(next);
                }
                else // Nothing was adjacent to anything in confirmed!
                    break;
            }
            
            return confirmed;
        }

        /// <summary>
        /// Create a rect that contains all given rects.
        /// </summary>
        public static Rect Encapsulate(this IEnumerable<Rect> rects, RectTransform.Axis? shrinkAxis = null)
        {
            if (!rects.Any())
                return default;

            float minX = shrinkAxis is RectTransform.Axis.Horizontal ? float.MinValue : float.MaxValue;
            float maxX = shrinkAxis is RectTransform.Axis.Horizontal ? float.MaxValue : float.MinValue;
            float minY = shrinkAxis is RectTransform.Axis.Vertical ? float.MinValue : float.MaxValue;
            float maxY = shrinkAxis is RectTransform.Axis.Vertical ? float.MaxValue : float.MinValue;
            
            foreach (var rect in rects)
            {
                switch (shrinkAxis)
                {
                    case RectTransform.Axis.Horizontal:
                        minX = Mathf.Max(minX, rect.xMin);
                        maxX = Mathf.Min(maxX, rect.xMax);
                        minY = Mathf.Min(minY, rect.yMin);
                        maxY = Mathf.Max(maxY, rect.yMax);
                        break;
                    case RectTransform.Axis.Vertical:
                        minX = Mathf.Min(minX, rect.xMin);
                        maxX = Mathf.Max(maxX, rect.xMax);
                        minY = Mathf.Max(minY, rect.yMin);
                        maxY = Mathf.Min(maxY, rect.yMax);
                        break;
                    case null:
                        minX = Mathf.Min(minX, rect.xMin);
                        maxX = Mathf.Max(maxX, rect.xMax);
                        minY = Mathf.Min(minY, rect.yMin);
                        maxY = Mathf.Max(maxY, rect.yMax);
                        break;
                }
            }

            if (minX > maxX)
                minX = maxX = (minX - maxX) * 0.5f;
            if (minY > maxY)
                minY = maxY = (minY - maxY) * 0.5f;
            
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}