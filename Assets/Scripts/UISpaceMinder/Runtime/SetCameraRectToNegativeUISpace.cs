using System;
using System.Linq;
using UISpaceMinder.Shims;
using UnityEngine;

namespace UISpaceMinder
{
    [RequireComponent(typeof(UISpaceMinder))]
    [ExecuteAlways]
    public class SetCameraRectToNegativeUISpace : MonoBehaviour
    {
        [SerializeField]
        [field: SerializeField] public Camera[] Cameras { get; set; } = Array.Empty<Camera>();

        private void OnEnable()
        {
            if (!TryGetComponent<UISpaceMinder>(out var minder))
                return;

            // Set camera rect on enable using last known negative space, if available
            if (!minder.NegativeSpace.Equals(NamedRectGroup.Empty))
                OnUserInterfaceChanged(minder.NegativeSpace, minder.LastKnownCanvas, 
                    minder.NegativeSpace.bounds.Normalize(minder.LastKnownCanvas));
                
            minder.NegativeSpaceChanged += OnUserInterfaceChanged;
        }

        private void OnDisable()
        {
            if (!TryGetComponent<UISpaceMinder>(out var minder))
                return;

            minder.NegativeSpaceChanged -= OnUserInterfaceChanged;
        }

        private void OnUserInterfaceChanged(NamedRectGroup space, Rect canvas, Rect normalizedBounds)
        {
            if (Cameras is not { Length: > 0 }) return;

            // Flip Y position (camera Y is opposite of UITK)
            normalizedBounds.y = 1 - normalizedBounds.height;
            foreach (var cam in Cameras.Where(c => c != null))
                cam.rect = normalizedBounds;
        }
    }
}
