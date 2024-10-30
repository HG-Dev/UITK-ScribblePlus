using System;
using System.Collections.Generic;
using System.Linq;
using Unity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UISpaceMinder.Inspector
{
    [CustomEditor(typeof(UISpaceMinder))]
    public class UISpaceMinderEditor : UnityEditor.Editor
    {
        private UISpaceMinder _minder;
        private MultiColumnListView _table;
        private VisualElement _positiveCanvas;
        private VisualElement _negativeCanvas;

        private void OnEnable()
        {
            _minder = target as UISpaceMinder;
            if (_minder == null)
                return;

            InitializeVisualizerTable();

            _minder.PositiveSpaceChanged += OnPositiveSpaceChanged;
            _minder.NegativeSpaceChanged += OnNegativeSpaceChanged;
        }

        private void OnDisable()
        {
            if (_minder == null)
                return;

            _minder.PositiveSpaceChanged -= OnPositiveSpaceChanged;
            _minder.NegativeSpaceChanged -= OnNegativeSpaceChanged;
        }

        private void OnNegativeSpaceChanged(NamedRectGroup space, Rect canvas, Rect normalizedBounds) => 
            ModifySpaceVisualization(_table, _negativeCanvas, 1, space, canvas);

        private void OnPositiveSpaceChanged(NamedRectGroup space, Rect canvas, Rect normalizedBounds) => 
            ModifySpaceVisualization(_table, _positiveCanvas, 0, space, canvas);

        private static void ModifySpaceVisualization(MultiColumnListView table, VisualElement visualizer, in int colIdx, in NamedRectGroup space, in Rect canvas)
        {
            if (canvas.height == 0)
            {
                PrepareChildElementCount(visualizer, 0);
                return;
            }

            var scalar = (128f / canvas.height);
            visualizer.style.width = canvas.width * scalar;
            table.columns[colIdx].width = canvas.width * scalar + 8;
            var children = PrepareChildElementCount(visualizer, space.collection.Length + 1);
            ApplyRectsToChildElements(children, space.collection, scalar);
            SetMaxBoundsChildElement(children[^1], space.bounds, scalar);
           
            table.style.width = table.columns.Sum(c => c.width.value) + 3;
            table.RefreshItems();
        }

        private static void SetMaxBoundsChildElement(VisualElement child, Rect bounds, float scalar)
        {
            child.style.backgroundColor = Color.clear;
            child.style.borderBottomWidth = 1;
            child.style.borderTopWidth = 1;
            child.style.borderLeftWidth = 1;
            child.style.borderRightWidth = 1;

            var position = bounds.position * scalar;
            var size = bounds.size * scalar;
            child.style.left = position.x;
            child.style.top = position.y;
            child.style.width = size.x;
            child.style.height = size.y;
        }

        private static void ApplyRectsToChildElements(IReadOnlyList<VisualElement> children, ReadOnlySpan<NamedRect> rects, float scalar)
        {
            for (int i = 0; i < rects.Length && i < children.Count; i++)
            {
                var rect = rects[i].rect;
                var position = rect.position * scalar;
                var size = rect.size * scalar;

                var colorIdx = Mathf.Abs(rects[i].name.GetHashCode()) % ColorPalette.Length;
                var child = children[i];
                child.style.backgroundColor = new StyleColor(ColorPalette[colorIdx]);
                child.style.position = Position.Absolute;
                child.style.left = position.x;
                child.style.top = position.y;
                child.style.width = size.x;
                child.style.height = size.y;
                child.style.borderLeftWidth = 0;
                child.style.borderTopWidth = 0;
                child.style.borderBottomWidth = 0;
                child.style.borderRightWidth = 0;
            }
        }

        private static IReadOnlyList<VisualElement> PrepareChildElementCount(VisualElement parent, int count)
        {
            var list = new List<VisualElement>();
            var idx = 0;
            foreach (var child in parent.Children())
            {
                if (idx >= count)
                {
                    child.visible = false;
                    continue;
                }
                    
                list.Add(child);
                child.visible = true;
                idx++;
            }
            for (; idx < count; idx++)
            {
                var child = new VisualElement()
                {
                    style =
                    {
                        flexGrow = 0,
                        flexShrink = 0,
                        borderBottomColor = Color.white,
                        borderLeftColor = Color.white,
                        borderRightColor = Color.white,
                        borderTopColor = Color.white,
                        borderBottomWidth = 0,
                        borderLeftWidth = 0,
                        borderRightWidth = 0,
                        borderTopWidth = 0,
                        position = Position.Absolute
                    }
                };
                parent.Add(child);
                list.Add(child);
            }
            return list;
        }

        private void InitializeVisualizerTable()
        {
            var posColumn = new Column()
            {
                title = "Positive",
                resizable = false,
                width = 128,
                sortable = false,
                stretchable = false,
                bindCell = (e, _) =>
                {
                    e.Add(_positiveCanvas);
                }
            };
            var negColumn = new Column()
            {
                title = "Negative",
                resizable = false,
                width = 128,
                sortable = false,
                stretchable = false,
                bindCell = (e, _) =>
                {
                    e.Add(_negativeCanvas);
                }
            };
            var cols = new Columns() { posColumn, negColumn };

            _table = new MultiColumnListView(cols)
            {
                focusable = false,
                pickingMode = PickingMode.Ignore,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                fixedItemHeight = 128,
                style = { flexGrow = 0, flexShrink = 0, width = 256 + 4, alignSelf = Align.Center },
                bindingSourceSelectionMode = BindingSourceSelectionMode.Manual,
                reorderable = false,
                sortingMode = ColumnSortingMode.Default,
                showBorder = true,
                horizontalScrollingEnabled = false,
            };

            _positiveCanvas = new SimpleCheckerboardBackground()
            {
                name = "Positive Canvas",
                focusable = false,
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width = new Length(128, LengthUnit.Pixel),
                    height = new Length(128, LengthUnit.Pixel)
                }
            };
            _negativeCanvas = new SimpleCheckerboardBackground()
            {
                name = "Negative Canvas",
                focusable = false,
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width = new Length(128, LengthUnit.Pixel),
                    height = new Length(128, LengthUnit.Pixel),
                }
            };
        }

        public override VisualElement CreateInspectorGUI()
        {
            var _inspector = new VisualElement()
            {
                focusable = false,
                style =
                {
                    marginBottom = 16,
                    marginTop = 16,
                    alignContent = Align.Center
                }
            };

            Debug.Assert(_table != null);
            _table.itemsSource = new byte[1] { 0 };

            _inspector.Add(_table);

            if (!_minder.PositiveSpace.Equals(NamedRectGroup.Empty))
                OnPositiveSpaceChanged(_minder.PositiveSpace, _minder.LastKnownCanvas, new Rect());
            if (!_minder.NegativeSpace.Equals(NamedRectGroup.Empty))
                OnNegativeSpaceChanged(_minder.NegativeSpace, _minder.LastKnownCanvas, new Rect());

            return _inspector;
        }

        private static readonly Color32[] ColorPalette = new[]
        {
            new Color32(12, 91, 176, 255),
            new Color32(238, 34, 16, 255),
            new Color32(20, 152, 61, 255),
            new Color32(236, 87, 154, 255),
            new Color32(20, 155, 237, 255),
            new Color32(250, 107, 9, 255),
            new Color32(21, 160, 140, 255),
            new Color32(254, 193, 12, 255)
        };
    }
}

