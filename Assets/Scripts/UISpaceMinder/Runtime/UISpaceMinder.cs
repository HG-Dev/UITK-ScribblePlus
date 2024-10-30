using System.Collections.Generic;
using System.Linq;
using System;
using UISpaceMinder.Shims;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UISpaceMinder
{
    /// <summary>
    /// Enumerates all visual elements in a UIDocument to obtain its positive (UI-covered) and negative space.
    /// </summary>
    /// <remarks>
    /// Must be placed under a UIDocument to receive UITK updates.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    [ExecuteAlways]
    public sealed class UISpaceMinder : MonoBehaviour
    {
        public delegate void UISpaceUpdateDelegate(NamedRectGroup space, Rect canvas, Rect normalizedBounds);
        public event UISpaceUpdateDelegate PositiveSpaceChanged;
        public event UISpaceUpdateDelegate NegativeSpaceChanged;

        public Rect LastKnownCanvas { get; private set; } = new Rect();
        public NamedRectGroup PositiveSpace { get; private set; } = NamedRectGroup.Empty;
        public NamedRectGroup NegativeSpace { get; private set; } = NamedRectGroup.Empty;

        private UIDocument _document;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("TypeSafety", "UNT0006:Incorrect message signature", Justification = "Awaitable is a valid signature")]
        private async Awaitable OnEnable()
        {
            _document = GetComponent<UIDocument>();

            while (_document.runtimePanel.isDirty)
                await Awaitable.EndOfFrameAsync(destroyCancellationToken);

            if (destroyCancellationToken.IsCancellationRequested)
                return;

            AnalyzeUIDocument(forceSendEvents: true);
        }

        public void AnalyzeUIDocument(bool forceSendEvents = false)
        {
            // Get all visible, named, opaque elements
            var blockers = _document.rootVisualElement.Query()
                .OfType<VisualElement>()
                .Where(IsVisible)
                .Build()
                .ToList();

            LastKnownCanvas = _document.rootVisualElement.worldBound;

            // Calculate positive space by aggregating element rects
            var positiveSpace = NamedRectGroup.Empty;

            {
                List<NamedRect> rects = new(blockers.Count * 2);

                foreach (var blocker in blockers)
                {
                    var (horizontalRect, verticalRect, maxBounds) = GetRectsFromVisualElement(blocker);

                    if (maxBounds.HasZeroArea() || !maxBounds.Overlaps(LastKnownCanvas))
                        continue;

                    // Which rects derived from this element are valid?
                    var isUnique = (horizontal: true, vertical: !verticalRect.Equals(horizontalRect));
                    if (rects.Count > 0)
                    {
                        isUnique = (
                            horizontal: !rects.Any(r => r.rect.Contains(horizontalRect)),
                            vertical: isUnique.vertical && !rects.Any(r => r.rect.Contains(verticalRect))
                        );
                    }

                    if (isUnique.horizontal)
                    {
                        // Remove all prexisting elements that could be contained by this
                        rects.RemoveAll(nr => horizontalRect.Contains(nr.rect));

                        rects.Add(new NamedRect(horizontalRect, blocker.name ?? maxBounds.center.ToString()));
                    }

                    if (isUnique.vertical)
                    {
                        rects.RemoveAll(nr => verticalRect.Contains(nr.rect));

                        rects.Add(new NamedRect(verticalRect, blocker.name ?? maxBounds.center.ToString()));
                    }
                }

                if (rects.Count > 0)
                {
                    positiveSpace = new NamedRectGroup(rects);
                }
            }

            // Calculate negative space by punching holes in canvas
            var negativeSpace = new NamedRectGroup(new[] { new NamedRect(LastKnownCanvas, "Canvas") });

            if (!positiveSpace.bounds.HasZeroArea())
            {
                var punchQueue = new Queue<Rect>();
                punchQueue.Enqueue(LastKnownCanvas);
                foreach (var uiRect in positiveSpace.collection)
                {
                    for (var queueCount = punchQueue.Count; queueCount > 0; queueCount--)
                    {
                        var significantPunchHoles = punchQueue
                            .Dequeue()
                            .Punch(uiRect.rect)
                            .Where(p => p.Area() > 4f);
                        foreach (var punched in significantPunchHoles)
                        {
                            punchQueue.Enqueue(punched);
                        }
                    }
                    if (punchQueue.Count == 0) break;
                }

                if (punchQueue.Any())
                {
                    Debug.Log("Punch queue contents:\n" + string.Join('\n', punchQueue));
                    negativeSpace = new NamedRectGroup(punchQueue.Encapsulate(), punchQueue.Select(r => new NamedRect(r)));
                    Debug.Log("Negative space bounds calculated to be: " + negativeSpace.bounds.ToString());
                }
            }

            var positiveSpaceNormalized = positiveSpace.bounds.Normalize(LastKnownCanvas);
            var negativeSpaceNormalized = negativeSpace.bounds.Normalize(LastKnownCanvas);

            if (forceSendEvents || !PositiveSpace.Equals(positiveSpace))
                PositiveSpaceChanged?.Invoke(positiveSpace, LastKnownCanvas, positiveSpaceNormalized);
            if (forceSendEvents || !NegativeSpace.Equals(negativeSpace))
                NegativeSpaceChanged?.Invoke(negativeSpace, LastKnownCanvas, negativeSpaceNormalized);

            PositiveSpace = positiveSpace;
            NegativeSpace = negativeSpace;
            return;

            static bool IsVisible(VisualElement element) =>
                element.visible && element.worldBound.Area() > 4 
                                && AncestorsAndSelf(element).All(e => Mathf.Approximately(e.resolvedStyle.opacity, 1))
                                && Mathf.Approximately(element.resolvedStyle.backgroundColor.a, 1);

            static IEnumerable<VisualElement> AncestorsAndSelf(VisualElement element)
            {
                do
                {
                    yield return element;
                    element = element.parent;
                } 
                while (element != null);
            }
        }

        static (Rect horizontal, Rect vertical, Rect maxBounds) GetRectsFromVisualElement(in VisualElement element)
        {
            // Reduce world bounds using margins
            var maxBounds = element.worldBound;
            maxBounds.xMax -= element.resolvedStyle.marginRight;
            maxBounds.xMin += element.resolvedStyle.marginLeft;
            maxBounds.yMax -= element.resolvedStyle.marginBottom;
            maxBounds.yMin += element.resolvedStyle.marginTop;

            // Further reduce by removing rounded corners
            var minBounds = new Rect(maxBounds);
            {
                var (tl, tr, br, bl) = (Mathf.Max(0, element.resolvedStyle.borderTopLeftRadius),
                    Mathf.Max(0, element.resolvedStyle.borderTopRightRadius),
                    Mathf.Max(0, element.resolvedStyle.borderBottomRightRadius),
                    Mathf.Max(0, element.resolvedStyle.borderBottomLeftRadius));

                minBounds.xMin += Mathf.Max(bl, tl);
                minBounds.yMin += Mathf.Max(tl, tr);
                minBounds.xMax -= Mathf.Max(tr, br);
                minBounds.yMax -= Mathf.Max(bl, br);
            }

            var horizontal = Rect.MinMaxRect(maxBounds.xMin, minBounds.yMin, maxBounds.xMax, minBounds.yMax);
            var vertical = Rect.MinMaxRect(minBounds.xMin, maxBounds.yMin, minBounds.xMax, maxBounds.yMax);

            return (horizontal, vertical, maxBounds);
        }
    }
}
