using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UISpaceMinder.Shims
{
    [Flags]
    public enum RectSides : byte
    {
        None = 0,
        XMin = 1 << 0,   // 1
        YMin = 1 << 1,   // 2
        XMax = 1 << 2,   // 4
        YMax = 1 << 3,   // 8
        X0Y0 = XMin | YMin,
        X1Y0 = XMax | YMin,
        X1Y1 = XMax | YMax,
        X0Y1 = XMin | YMax,
        All = YMax | XMax | YMin | XMin
    }

    public enum RectAxis : byte
    {
        None,
        X,
        Y
    }
    
    public static class RectExtensions
    {
        public static bool HasZeroArea(this Rect self) => 
            Mathf.Approximately(self.width, 0) || Mathf.Approximately(self.height, 0);

        public static bool Contains(this Rect self, Rect other) => other.yMin >= self.yMin && other.yMax <= self.yMax
                && other.xMin >= self.xMin && other.xMax <= self.xMax;

        public static Rect Normalize(this Rect self, Rect limits)
        {
            var xMin = Mathf.InverseLerp(limits.xMin, limits.xMax, self.xMin);
            var xMax = Mathf.InverseLerp(limits.xMin, limits.xMax, self.xMax);
            var yMin = Mathf.InverseLerp(limits.yMin, limits.yMax, self.yMin);
            var yMax = Mathf.InverseLerp(limits.yMin, limits.yMax, self.yMax);

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

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
        public static bool OverlapsOrTouchesSides(this Rect self, Rect other, out RectSides sides)
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
                .Where(value => Mathf.IsPowerOfTwo((int)value) && sides.HasFlag(value));

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
                        throw new ArgumentOutOfRangeException(paramName: nameof(test), $"Should be one flag: {test}");
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
                var next = unconfirmed.FirstOrDefault(u => confirmed.Any(c => u.OverlapsOrTouchesSides(c, out _)));
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
        /// Returns a modified collection of Rect such that their minimum and maximum values on 'shrinkAxis'
        /// are all equivalent.
        /// Returns the standard intersection, if any, of all rects when shrinkAxis is None.
        /// </summary>
        public static Rect[] IntersectSliceMany(this IEnumerable<Rect> rects, RectAxis sliceAxis = RectAxis.None)
        {
            if (!rects.Any())
                return Array.Empty<Rect>();

            float minX = sliceAxis is RectAxis.X ? float.MinValue : float.MaxValue;
            float maxX = sliceAxis is RectAxis.X ? float.MaxValue : float.MinValue;
            float minY = sliceAxis is RectAxis.Y ? float.MinValue : float.MaxValue;
            float maxY = sliceAxis is RectAxis.Y ? float.MaxValue : float.MinValue;

            // Aggregate limits
            var count = 0;
            foreach (var rect in rects)
            {
                count++;
                switch (sliceAxis)
                {
                    case RectAxis.X:
                        minX = Mathf.Max(minX, rect.xMin);
                        maxX = Mathf.Min(maxX, rect.xMax);
                        minY = Mathf.Min(minY, rect.yMin);
                        maxY = Mathf.Max(maxY, rect.yMax);
                        break;
                    case RectAxis.Y:
                        minX = Mathf.Min(minX, rect.xMin);
                        maxX = Mathf.Max(maxX, rect.xMax);
                        minY = Mathf.Max(minY, rect.yMin);
                        maxY = Mathf.Min(maxY, rect.yMax);
                        break;
                    case RectAxis.None:
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

            if (sliceAxis is RectAxis.None) // Return basic intersection
                return new Rect[] { Rect.MinMaxRect(minX, minY, maxX, maxY) };

            Rect[] output = new Rect[count];
            var i = 0;
            // Apply limits to input rects
            foreach (var rect in rects)
            {
                var rectMinX = Mathf.Max(minX, rect.xMin);
                var rectMaxX = Mathf.Min(maxX, rect.xMax);
                var rectMinY = Mathf.Max(minY, rect.yMin);
                var rectMaxY = Mathf.Min(maxY, rect.yMax);
                output[i++] = Rect.MinMaxRect(rectMinX, rectMinY, rectMaxX, rectMaxY);
            }

            return output;
        }

        /// <summary>
        /// Return an rect expanded to contain both alpha and beta.
        /// </summary>
        public static Rect Encapsulate(this Rect alpha, Rect beta) => Rect.MinMaxRect(
                Mathf.Min(alpha.xMin, beta.xMin),
                Mathf.Min(alpha.yMin, beta.yMin),
                Mathf.Max(alpha.xMax, beta.xMax),
                Mathf.Max(alpha.yMax, beta.yMax));

        /// <summary>
        /// Create a rect that contains all given rects.
        /// </summary>
        public static Rect Encapsulate(this IEnumerable<Rect> rects)
        {
            Rect output = default;

            // Test plurality
            using var iterator = rects.GetEnumerator();
            
            if (!iterator.MoveNext())
                throw new ArgumentException("Cannot encapsulate empty collection of rects", paramName: nameof(rects));

            output = iterator.Current;
            while (iterator.MoveNext())
                output = output.Encapsulate(iterator.Current);

            return output;
        }

        private static bool TryPunchInternal(in Rect canvas, in Rect remove, out Rect intersect, out RectSides sections)
        {
            intersect = canvas;
            sections = RectSides.None;

            if (!canvas.Overlaps(remove))
                return false;

            intersect = Rect.MinMaxRect(
                Mathf.Max(canvas.xMin, remove.xMin),
                Mathf.Max(canvas.yMin, remove.yMin),
                Mathf.Min(canvas.xMax, remove.xMax),
                Mathf.Min(canvas.yMax, remove.yMax)
            );

            if (intersect.xMin > canvas.xMin)
                sections |= RectSides.XMin;
            if (intersect.xMax < canvas.xMax)
                sections |= RectSides.XMax;
            if (intersect.yMin > canvas.yMin)
                sections |= RectSides.YMin;
            if (intersect.yMax < canvas.yMax)
                sections |= RectSides.YMax;

            return true;
        }
        
        private static readonly RectSides[] PunchSections = new RectSides[8]
            {
                RectSides.X0Y0 ,
                RectSides.YMin ,
                RectSides.X1Y0 ,
                RectSides.XMax ,
                RectSides.X1Y1 ,
                RectSides.YMax ,
                RectSides.X0Y1 ,
                RectSides.XMin 
            };

        public static List<Rect> Punch(this Rect canvas, Rect remove)
        {
            List<Rect> result = new (8);

            // Check if the rectangles intersect
            if (!TryPunchInternal(canvas, remove, out Rect intersect, out RectSides sections))
            {
                // If not, return the canvas unaltered
                result.Add(canvas);
                return result;
            }

            // Check if the punch fully deletes canvas
            if (intersect.Equals(canvas))
                return result;

            foreach (var subsection in PunchSections)
            {
                if (!sections.HasFlag(subsection)) continue;

                result.Add( GetPunchSubsection(canvas, intersect, subsection) );
            }

            return result;

            static Rect GetPunchSubsection(in Rect canvas, in Rect intersect, in RectSides section) => section switch
            {
                // Assume gap between canvas and intersect yMin
                RectSides.YMin => Rect.MinMaxRect(intersect.xMin, canvas.yMin, intersect.xMax, intersect.yMin),
                RectSides.XMax => Rect.MinMaxRect(intersect.xMax, intersect.yMin, canvas.xMax, intersect.yMax),
                RectSides.YMax => Rect.MinMaxRect(intersect.xMin, intersect.yMax, intersect.xMax, canvas.yMax),
                RectSides.XMin => Rect.MinMaxRect(canvas.xMin, intersect.yMin, intersect.xMin, intersect.yMax),
                RectSides.X0Y0 => Rect.MinMaxRect(canvas.xMin, canvas.yMin, intersect.xMin, intersect.yMin),
                RectSides.X1Y0 => Rect.MinMaxRect(intersect.xMax, canvas.yMin, canvas.xMax, intersect.yMin),
                RectSides.X1Y1 => Rect.MinMaxRect(intersect.xMax, intersect.yMax, canvas.xMax, canvas.yMax),
                RectSides.X0Y1 => Rect.MinMaxRect(canvas.xMin, intersect.yMax, intersect.xMin, canvas.yMax),
                _ => throw new ArgumentOutOfRangeException(nameof(section)),
            };
        }
    }
}