using MatchingPair.Gameplay.GridSystem;
using UnityEngine;

namespace MatchingPair.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class Camera_Controller : MonoBehaviour
    {
        #region Fields
        [SerializeField] private CardGridSystem cardGridSystem;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private bool fitOnStart = true;
        [SerializeField] private bool fitWhenLevelBuilt = true;
        [SerializeField, Min(0f)] private float paddingWorldUnits = 0.5f;

        private Camera cachedCamera;
        #endregion

        #region UnityCallbacks
        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (levelManager != null)
            {
                levelManager.OnLevelBuilt += HandleLevelBuilt;
            }
        }

        private void Start()
        {
            if (!fitOnStart)
            {
                return;
            }

            FitToGrid();
        }

        private void OnDisable()
        {
            if (levelManager != null)
            {
                levelManager.OnLevelBuilt -= HandleLevelBuilt;
            }
        }
        #endregion

        #region PublicAPI
        public void FitToGrid()
        {
            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }

            if (cachedCamera == null || cardGridSystem == null || cardGridSystem.Settings == null)
            {
                return;
            }

            GridSettings settings = cardGridSystem.Settings;
            settings.Validate();

            Vector3 bottomLeft;
            Vector3 bottomRight;
            Vector3 topLeft;
            Vector3 topRight;
            CalculateGridWorldCorners(settings, out bottomLeft, out bottomRight, out topLeft, out topRight);

            Vector3 gridCenter = settings.Origin;
            Vector3 cameraForward = cachedCamera.transform.forward;
            float signedDepth = Vector3.Dot(cachedCamera.transform.position - gridCenter, cameraForward);
            cachedCamera.transform.position = gridCenter + cameraForward * signedDepth;

            float minLocalX;
            float maxLocalX;
            float minLocalY;
            float maxLocalY;
            CalculateViewBounds(bottomLeft, bottomRight, topLeft, topRight, out minLocalX, out maxLocalX, out minLocalY, out maxLocalY);

            float halfWidth = ((maxLocalX - minLocalX) * 0.5f) + paddingWorldUnits;
            float halfHeight = ((maxLocalY - minLocalY) * 0.5f) + paddingWorldUnits;
            float safeAspect = Mathf.Max(0.0001f, cachedCamera.aspect);

            if (cachedCamera.orthographic)
            {
                float targetOrthoSize = Mathf.Max(halfHeight, halfWidth / safeAspect);
                cachedCamera.orthographicSize = Mathf.Max(0.01f, targetOrthoSize);
                return;
            }

            float halfVerticalFov = cachedCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float halfHorizontalFov = Mathf.Atan(Mathf.Tan(halfVerticalFov) * safeAspect);
            float requiredDistanceForHeight = halfHeight / Mathf.Max(0.0001f, Mathf.Tan(halfVerticalFov));
            float requiredDistanceForWidth = halfWidth / Mathf.Max(0.0001f, Mathf.Tan(halfHorizontalFov));
            float requiredDistance = Mathf.Max(requiredDistanceForHeight, requiredDistanceForWidth);
            cachedCamera.transform.position = gridCenter - cameraForward * requiredDistance;
        }
        #endregion

        #region PrivateHelpers
        private void HandleLevelBuilt(LevelContext _)
        {
            if (!fitWhenLevelBuilt)
            {
                return;
            }

            FitToGrid();
        }

        private void CalculateGridWorldCorners(
            GridSettings settings,
            out Vector3 bottomLeft,
            out Vector3 bottomRight,
            out Vector3 topLeft,
            out Vector3 topRight)
        {
            Vector3 cellSize = GridMath.GetEffectiveCellSize(settings);
            Vector2 step = GridMath.GetCellStep(settings);
            Vector2 startOffset = GridMath.CalculateGridStartOffset(settings);

            float halfCellWidth = cellSize.x * 0.5f;
            float halfCellHeight = settings.Plane == GridPlane.XZ ? cellSize.z * 0.5f : cellSize.y * 0.5f;

            float left = startOffset.x - halfCellWidth;
            float right = startOffset.x + ((settings.Width - 1) * step.x) + halfCellWidth;
            float top = startOffset.y + halfCellHeight;
            float bottom = startOffset.y - ((settings.Height - 1) * step.y) - halfCellHeight;

            Vector3 origin = settings.Origin;
            if (settings.Plane == GridPlane.XZ)
            {
                bottomLeft = new Vector3(origin.x + left, origin.y, origin.z + bottom);
                bottomRight = new Vector3(origin.x + right, origin.y, origin.z + bottom);
                topLeft = new Vector3(origin.x + left, origin.y, origin.z + top);
                topRight = new Vector3(origin.x + right, origin.y, origin.z + top);
                return;
            }

            bottomLeft = new Vector3(origin.x + left, origin.y + bottom, origin.z);
            bottomRight = new Vector3(origin.x + right, origin.y + bottom, origin.z);
            topLeft = new Vector3(origin.x + left, origin.y + top, origin.z);
            topRight = new Vector3(origin.x + right, origin.y + top, origin.z);
        }

        private void CalculateViewBounds(
            Vector3 bottomLeft,
            Vector3 bottomRight,
            Vector3 topLeft,
            Vector3 topRight,
            out float minLocalX,
            out float maxLocalX,
            out float minLocalY,
            out float maxLocalY)
        {
            Vector3 localBottomLeft = cachedCamera.transform.InverseTransformPoint(bottomLeft);
            Vector3 localBottomRight = cachedCamera.transform.InverseTransformPoint(bottomRight);
            Vector3 localTopLeft = cachedCamera.transform.InverseTransformPoint(topLeft);
            Vector3 localTopRight = cachedCamera.transform.InverseTransformPoint(topRight);

            minLocalX = localBottomLeft.x;
            maxLocalX = localBottomLeft.x;
            minLocalY = localBottomLeft.y;
            maxLocalY = localBottomLeft.y;

            ExpandBounds(localBottomRight, ref minLocalX, ref maxLocalX, ref minLocalY, ref maxLocalY);
            ExpandBounds(localTopLeft, ref minLocalX, ref maxLocalX, ref minLocalY, ref maxLocalY);
            ExpandBounds(localTopRight, ref minLocalX, ref maxLocalX, ref minLocalY, ref maxLocalY);
        }

        private static void ExpandBounds(
            Vector3 localPoint,
            ref float minLocalX,
            ref float maxLocalX,
            ref float minLocalY,
            ref float maxLocalY)
        {
            if (localPoint.x < minLocalX)
            {
                minLocalX = localPoint.x;
            }

            if (localPoint.x > maxLocalX)
            {
                maxLocalX = localPoint.x;
            }

            if (localPoint.y < minLocalY)
            {
                minLocalY = localPoint.y;
            }

            if (localPoint.y > maxLocalY)
            {
                maxLocalY = localPoint.y;
            }
        }
        #endregion
    }
}
