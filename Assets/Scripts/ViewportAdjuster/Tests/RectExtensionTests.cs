using NUnit.Framework;
using System;
using UnityEngine;
using ViewportAdjuster.Shims;
// ReSharper disable InconsistentNaming

public class RectExtensionTests
{
    private static readonly Rect Baseline = new Rect(0, 0, 10, 10);
    private static readonly Rect NotAdjacent = new Rect(10, 10, 10, 10);
    private static readonly Rect YMinPartialAdjacent = new Rect(5, -10, 10, 10);
    private static readonly Rect YMaxPartialAdjacent = new Rect(-5, 10, 10, 10);
    private static readonly Rect XMinPartialAdjacent = new Rect(-10, -5, 10, 10);
    private static readonly Rect XMaxPartialAdjacent = new Rect(10, 5, 10, 10);
    private static Rect[] PartialAdjacents = 
        { YMinPartialAdjacent, XMinPartialAdjacent, YMaxPartialAdjacent, XMaxPartialAdjacent };
    private static readonly Rect YMinPartialOverlap = new Rect(5, -5, 10, 10);
    private static readonly Rect YMaxPartialOverlap = new Rect(-5, 5, 10, 10);
    private static readonly Rect XMinPartialOverlap = new Rect(-5, -5, 10, 10);
    private static readonly Rect XMaxPartialOverlap = new Rect(5, 5, 10, 10);
    private static readonly Rect YMinFullAdjacent = new Rect(0, -10, 10, 10);
    private static readonly Rect YMaxFullAdjacent = new Rect(0, 10, 10, 10);
    private static readonly Rect XMinFullAdjacent = new Rect(-10, 0, 10, 10);
    private static readonly Rect XMaxFullAdjacent = new Rect(10, 0, 10, 10);
    
       // -10,-10              ████████████████           
       //                      ██5,-10       ██           
       // ████████████████     ██            ██           
       // ██-10,-5      ██     ██            ██           
       // ██            ██     ██YMinPartial ██           
       // ██            ███████████████████████           
       // ██XMinPartial ██0,0         ██                
       // ████████████████            ██                
       //               ██            ████████████████       
       //               ██       10,10██10,5        ██       
       //        ███████████████████████            ██       
       //        ██-5,10       ██     ██            ██       
       //        ██            ██     ██XMaxPartial ██       
       //        ██            ██     ████████████████       
       //        ██YMaxPartial ██                      
       //        ████████████████                20,20                     

    [Test]
    public void OverlapsHorizontally()
    {
        Assert.IsFalse(NotAdjacent.OverlapsHorizontally(Baseline));
        // XMin / XMax are fully left and right of baseline
        Assert.IsFalse(XMinFullAdjacent.OverlapsHorizontally(Baseline));
        Assert.IsFalse(XMaxFullAdjacent.OverlapsHorizontally(Baseline));
        Assert.IsTrue(YMinFullAdjacent.OverlapsHorizontally(Baseline));
        Assert.IsTrue(YMaxFullAdjacent.OverlapsHorizontally(Baseline));
        Assert.IsFalse(XMinPartialAdjacent.OverlapsHorizontally(Baseline));
        Assert.IsFalse(XMaxPartialAdjacent.OverlapsHorizontally(Baseline));
        Assert.IsTrue(YMinPartialAdjacent.OverlapsHorizontally(Baseline));
        Assert.IsTrue(YMaxPartialAdjacent.OverlapsHorizontally(Baseline));
    }
    
    [Test]
    public void OverlapsVertically()
    {
        Assert.IsFalse(NotAdjacent.OverlapsVertically(Baseline));
        // XMin / XMax are fully left and right of baseline
        Assert.IsTrue(XMinFullAdjacent.OverlapsVertically(Baseline));
        Assert.IsTrue(XMaxFullAdjacent.OverlapsVertically(Baseline));
        Assert.IsFalse(YMinFullAdjacent.OverlapsVertically(Baseline));
        Assert.IsFalse(YMaxFullAdjacent.OverlapsVertically(Baseline));
        Assert.IsTrue(XMinPartialAdjacent.OverlapsVertically(Baseline));
        Assert.IsTrue(XMaxPartialAdjacent.OverlapsVertically(Baseline));
        Assert.IsFalse(YMinPartialAdjacent.OverlapsVertically(Baseline));
        Assert.IsFalse(YMaxPartialAdjacent.OverlapsVertically(Baseline));
    }
    
    [Test]
    public void TouchesSideApproximately()
    {
        Assert.IsTrue(YMaxPartialAdjacent.TouchesSideApproximately(Baseline, RectSides.YMax));
        Assert.IsTrue(XMaxPartialAdjacent.TouchesSideApproximately(Baseline, RectSides.XMax));
        Assert.IsTrue(YMinPartialAdjacent.TouchesSideApproximately(Baseline, RectSides.YMin));
        Assert.IsTrue(XMinPartialAdjacent.TouchesSideApproximately(Baseline, RectSides.XMin));
        Assert.IsTrue(YMaxFullAdjacent.TouchesSideApproximately(Baseline, RectSides.YMax));
        Assert.IsTrue(XMaxFullAdjacent.TouchesSideApproximately(Baseline, RectSides.XMax));
        Assert.IsTrue(YMinFullAdjacent.TouchesSideApproximately(Baseline, RectSides.YMin));
        Assert.IsTrue(XMinFullAdjacent.TouchesSideApproximately(Baseline, RectSides.XMin));
        Assert.IsFalse(YMaxPartialOverlap.TouchesSideApproximately(Baseline, RectSides.YMax));
        Assert.IsFalse(XMaxPartialOverlap.TouchesSideApproximately(Baseline, RectSides.XMax));
        Assert.IsFalse(YMinPartialOverlap.TouchesSideApproximately(Baseline, RectSides.YMin));
        Assert.IsFalse(XMinPartialOverlap.TouchesSideApproximately(Baseline, RectSides.XMin));
        Assert.IsFalse(NotAdjacent.TouchesSideApproximately(Baseline, RectSides.XMax));
    }
    
    [Test]
    public void OverlapsOrTouches()
    {
        Assert.IsTrue(YMaxPartialAdjacent.OverlapsOrTouches(Baseline, out var sides));
        Assert.IsTrue(sides is RectSides.YMax, $"Comparison of {YMaxPartialAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.YMax}, but it was {sides}");
        Assert.IsTrue(XMaxPartialAdjacent.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is RectSides.XMax, $"Comparison of {XMaxPartialAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.XMax}, but it was {sides}");
        Assert.IsTrue(YMinPartialAdjacent.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is RectSides.YMin, $"Comparison of {YMinPartialAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.YMin}, but it was {sides}");
        Assert.IsTrue(XMinPartialAdjacent.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is RectSides.XMin, $"Comparison of {XMinPartialAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.XMin}, but it was {sides}");
        Assert.IsTrue(YMaxFullAdjacent.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is RectSides.YMax, $"Comparison of {YMinFullAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.YMax}, but it was {sides}");
        Assert.IsTrue(XMaxFullAdjacent.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is RectSides.XMax, $"Comparison of {XMaxFullAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.XMax}, but it was {sides}");
        Assert.IsTrue(YMinFullAdjacent.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is RectSides.YMin, $"Comparison of {YMinFullAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.YMin}, but it was {sides}");
        Assert.IsTrue(XMinFullAdjacent.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is RectSides.XMin, $"Comparison of {XMinFullAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.XMin}, but it was {sides}");
        Assert.IsTrue(YMaxPartialOverlap.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is (RectSides.YMax | RectSides.XMin), $"Comparison of {YMinPartialOverlap} to baseline {Baseline}\nExpected sides to be {(RectSides.YMax | RectSides.XMin)}, but it was {sides}");
        Assert.IsTrue(XMaxPartialOverlap.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is (RectSides.XMax | RectSides.YMax), $"Comparison of {XMaxPartialOverlap} to baseline {Baseline}\nExpected sides to be {(RectSides.XMax | RectSides.YMax)}, but it was {sides}");
        Assert.IsTrue(YMinPartialOverlap.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is (RectSides.YMin | RectSides.XMax), $"Comparison of {YMinPartialOverlap} to baseline {Baseline}\nExpected sides to be {(RectSides.YMin | RectSides.XMax)}, but it was {sides}");
        Assert.IsTrue(XMinPartialOverlap.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is (RectSides.XMin | RectSides.YMin), $"Comparison of {XMinPartialOverlap} to baseline {Baseline}\nExpected sides to be {(RectSides.XMin | RectSides.YMin)}, but it was {sides}");
        Assert.IsFalse(NotAdjacent.OverlapsOrTouches(Baseline, out sides));
        Assert.IsTrue(sides is RectSides.None, $"Comparison of {NotAdjacent} to baseline {Baseline}\nExpected sides to be {RectSides.None}, but it was {sides}");
    }

    [Test]
    public void EncapsulateNoShrinking()
    {
        var partialsEncapsulated = PartialAdjacents.Encapsulate();
        Assert.IsTrue(partialsEncapsulated.Equals(new Rect(-10, -10, 30, 30)));
        var XMinYMaxEncapsulated = PartialAdjacents[1..3].Encapsulate();
        Assert.IsTrue(XMinYMaxEncapsulated.Equals(Rect.MinMaxRect(-10, -5, 5, 20)));
        var nothingEncapsulated = Array.Empty<Rect>().Encapsulate();
        Assert.IsTrue(nothingEncapsulated.Equals(default));
    }
    
    [Test]
    public void EncapsulateShrinkHorizontally()
    {
        var partialsEncapsulated = PartialAdjacents.Encapsulate(RectTransform.Axis.Horizontal);
        Assert.IsTrue(partialsEncapsulated.Equals(new Rect(5, -10, 0, 30)));
        var XMinYMaxEncapsulated = PartialAdjacents[1..3].Encapsulate(RectTransform.Axis.Horizontal);
        Assert.IsTrue(XMinYMaxEncapsulated.Equals(Rect.MinMaxRect(-5, -5, 0, 20)));
    }
    
    [Test]
    public void EncapsulateShrinkVertically()
    {
        var partialsEncapsulated = PartialAdjacents.Encapsulate(RectTransform.Axis.Vertical);
        Assert.IsTrue(partialsEncapsulated.Equals(new Rect(-10, 5, 30, 0)));
        var XMinYMinEncapsulated = PartialAdjacents[..2].Encapsulate(RectTransform.Axis.Vertical);
        Assert.IsTrue(XMinYMinEncapsulated.Equals(Rect.MinMaxRect(-10, -5, 15, 0)));
    }
}
