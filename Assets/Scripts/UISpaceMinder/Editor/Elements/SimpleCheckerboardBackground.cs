using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI
{
    /// <summary>
    /// A simplified version of CheckboardBackground used by the UnityEditor
    /// </summary>
    public class SimpleCheckerboardBackground : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new SimpleCheckerboardBackground();
        }

        static readonly CustomStyleProperty<int> TextureSizeProperty = new CustomStyleProperty<int>("--texture-size");
        static readonly CustomStyleProperty<Color> OddCellColorProperty = new CustomStyleProperty<Color>("--odd-cell-color");
        static readonly CustomStyleProperty<Color> EvenCellColorProperty = new CustomStyleProperty<Color>("--even-cell-color");

        static readonly Color k_DefaultOddCellColor = new Color(0f, 0f, 0f, 0.18f);
        static readonly Color k_DefaultEvenCellColor = new Color(0f, 0f, 0f, 0.38f);

        int _textureSize = 16;
        Color _oddCellColor = k_DefaultOddCellColor;
        Color _evenCellColor = k_DefaultEvenCellColor;

        Texture2D _texture;

        public SimpleCheckerboardBackground()
        {
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            style.backgroundColor = k_DefaultEvenCellColor;
        }

        ~SimpleCheckerboardBackground()
        {
            DestroyTexture();
        }

        void DestroyTexture()
        {
            if (_texture != null)
                Object.DestroyImmediate(_texture);

            _texture = null;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_texture == null)
                GenerateResources();
        }


        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            bool generateResources = false;

            if (e.customStyle.TryGetValue(TextureSizeProperty, out var textureSizeProperty))
            {
                if (_textureSize != textureSizeProperty)
                {
                    _textureSize = textureSizeProperty;
                    generateResources = true;
                }
            }

            if (e.customStyle.TryGetValue(OddCellColorProperty, out var oddCellColor))
            {
                if (_oddCellColor != oddCellColor)
                {
                    _oddCellColor = oddCellColor;
                    generateResources = true;
                }
            }

            if (e.customStyle.TryGetValue(EvenCellColorProperty, out var evenCellColor))
            {
                if (_evenCellColor != evenCellColor)
                {
                    _evenCellColor = evenCellColor;
                    generateResources = true;
                }
            }

            if (generateResources || _texture == null)
            {
                GenerateResources();
            }
        }

        void GenerateResources()
        {
            style.backgroundImage = new Background();
            DestroyTexture();

            _texture = new Texture2D(_textureSize, _textureSize)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            var quadrantSize = Vector2.one * 0.5f * _textureSize;
            var quadrants = new Rect[4]
            {
                new Rect(quadrantSize, quadrantSize),
                new Rect(new Vector2(0, quadrantSize.y), quadrantSize),
                new Rect(Vector2.zero, quadrantSize),
                new Rect(new Vector2(quadrantSize.x, 0), quadrantSize),
            };

            Func<int, int, Color> getColor = (x, y) =>
            {
                for (int i = 0; i < quadrants.Length; i++)
                    if (quadrants[i].Contains(new Vector2(x, y)))
                        return i % 2 is 0 ? _evenCellColor : _oddCellColor;
                return Color.magenta;
            };

            for (var x = 0; x < _textureSize; x++)
            {
                for (var y = 0; y < _textureSize; y++)
                {
                    _texture.SetPixel(x, y, getColor(x, y));
                }
            }

            _texture.Apply(false, true);

            style.backgroundColor = Color.clear;
            style.backgroundImage = Background.FromTexture2D(_texture);
            style.backgroundSize = new BackgroundSize(_textureSize, _textureSize);
            style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            DestroyTexture();
        }
    }
}