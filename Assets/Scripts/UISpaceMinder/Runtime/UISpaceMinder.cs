using System.Collections.Generic;
using System.Linq;
using System;
using UISpaceMinder.Shims;
using UnityEngine;
using UnityEngine.UIElements;

namespace UISpaceMinder
{
    /// <summary>
    /// Obtains the positive (UI-covered) and negative space in a UIDocument.
    /// </summary>
    /// <remarks>
    /// Must be placed under a UIDocument to recieve UITK updates.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    [ExecuteAlways]
    public class UISpaceMinder : MonoBehaviour
    {
        private UIDocument _document;
        private RectGroup _previousPositiveSpace = RectGroup.Empty;
        private RectGroup _previousNegativeSpace = RectGroup.Empty;

        [Header("Sync Camera Rect to Negative UI Space")]
        [SerializeField]
        private Camera[] _targetCameras = Array.Empty<Camera>();

        public delegate void UISpaceUpdateDelegate(RectGroup space, Rect canvas, Rect normalizedBounds);
        public event UISpaceUpdateDelegate PositiveSpaceChanged;
        public event UISpaceUpdateDelegate NegativeSpaceChanged;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("TypeSafety", "UNT0006:Incorrect message signature", Justification = "Awaitable is a valid return signature")]
        private async Awaitable OnEnable()
        {
            _document = GetComponent<UIDocument>();

            while (_document.runtimePanel.isDirty)
                await Awaitable.EndOfFrameAsync(destroyCancellationToken);

            if (destroyCancellationToken.IsCancellationRequested)
                return;

            // Get all visible, named, opaque elements
            var blockers = _document.rootVisualElement.Query()
                .OfType<VisualElement>()
                .Where(IsVisibleAndOpaque)
                .Build()
                .ToList();

            var canvas = _document.rootVisualElement.worldBound;

            // Calculate positive space by aggregating element rects
            var positiveSpace = RectGroup.Empty;

            {
                Rect bounds = canvas;
                List<Rect> rects = new List<Rect>();

                var validElementRects = blockers
                    .Select(blocker => GetRectsFromVisualElement(blocker))
                    .Where(r => !r.maxBounds.HasZeroArea() && r.maxBounds.Overlaps(canvas));

                foreach (var (horizontal, vertical, maxBounds) in validElementRects)
                {
                    var isUnique = (horizontal: true, vertical: !vertical.Equals(horizontal));
                    if (bounds.Equals(canvas))
                    {
                        bounds = maxBounds;
                    }
                    else
                    {
                        bounds = bounds.Encapsulate(maxBounds);
                        isUnique = (
                            horizontal: !rects.Any(r => r.Contains(horizontal)), 
                            vertical: isUnique.vertical && !rects.Any(r => r.Contains(vertical))
                        );
                    }
                    
                    if (isUnique.horizontal)
                    {
                        // Remove all prexisting elements that could be contained by this
                        rects.RemoveAll(rect => horizontal.Contains(rect));
                        rects.Add(horizontal);
                    }
                    
                    if (isUnique.vertical)
                    {
                        rects.RemoveAll(rect => vertical.Contains(rect));
                        rects.Add(vertical);
                    }
                }

                if (rects.Count > 0)
                {
                    positiveSpace = new RectGroup(bounds, rects.ToArray());
                }
            }

            // Calculate negative space by punching holes in canvas
            var negativeSpace = new RectGroup(canvas, new[] { canvas });

            if (!positiveSpace.bounds.HasZeroArea())
            {
                var punchQueue = new Queue<Rect>();
                punchQueue.Enqueue(canvas);
                foreach (var uiRect in positiveSpace.collection)
                {
                    for (var queueCount = punchQueue.Count; queueCount > 0; queueCount--)
                    {
                        foreach (var punched in punchQueue.Dequeue().Punch(uiRect))
                        {
                            punchQueue.Enqueue(punched);
                        }
                    }
                    if (punchQueue.Count == 0) break;
                }

                if (punchQueue.Any())
                {
                    negativeSpace = new RectGroup(punchQueue.Encapsulate(), punchQueue.ToArray());
                }
            }

            Debug.Log($"Calculation of positive and negative space finished.\nCanvas: {canvas}\n\tPos:\n{positiveSpace}\n\tNeg:\n{negativeSpace}");

            var positiveSpaceNormalized = positiveSpace.bounds.Normalize(canvas);
            var negativeSpaceNormalized = negativeSpace.bounds.Normalize(canvas);

            if (!_previousPositiveSpace.Equals(positiveSpace))
                PositiveSpaceChanged?.Invoke(positiveSpace, canvas, positiveSpaceNormalized);
            if (!_previousNegativeSpace.Equals(negativeSpace))
                NegativeSpaceChanged?.Invoke(negativeSpace, canvas, negativeSpaceNormalized);

            foreach (var cam in _targetCameras.Where(c => c != null))
                cam.rect = negativeSpaceNormalized;

            _previousPositiveSpace = positiveSpace;
            _previousNegativeSpace = negativeSpace;
            Debug.Log("Finished");
        }


        static bool IsVisibleAndOpaque(VisualElement element)
        {
            return element.visible
                   //&& !string.IsNullOrEmpty(element.name)
                   && Mathf.Approximately(
                       element.resolvedStyle.backgroundColor.a * element.resolvedStyle.opacity, 1);
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
                var (tl, tr, br, bl) = (element.resolvedStyle.borderTopLeftRadius,
                    element.resolvedStyle.borderTopRightRadius,
                    element.resolvedStyle.borderBottomRightRadius,
                    element.resolvedStyle.borderBottomLeftRadius);

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
