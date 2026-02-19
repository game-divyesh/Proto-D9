using System;
using UnityEngine;

namespace MatchingPair.Gameplay.GridSystem
{
    public sealed class CardGridSystem : MonoBehaviour
    {
        #region Fields
        [SerializeField] private GridSettings settings = new GridSettings();

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoCellColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color gizmoOriginColor = new Color(1f, 0.5f, 0f, 1f);
        #endregion

        #region Properties
        public GridSettings Settings => settings;
        #endregion

        #region Unity
        private void Awake()
        {
            EnsureSettings();
        }

        private void OnValidate()
        {
            EnsureSettings();
            settings.Validate();
        }
        #endregion

        #region PublicAPI
        public Vector3 GetWorldPosition(int x, int y)
        {
            EnsureSettings();
            return GridMath.GridToWorld(settings, new GridCoordinate(x, y));
        }

        public bool TryGetCellFromWorld(Vector3 worldPosition, out GridCoordinate coordinate)
        {
            EnsureSettings();

            GridCoordinate mappedCoordinate = GridMath.WorldToGrid(settings, worldPosition, false);
            if (!GridMath.IsWithinBounds(settings, mappedCoordinate))
            {
                coordinate = default;
                return false;
            }

            coordinate = mappedCoordinate;
            return true;
        }

        public void PlaceAtCell(Transform target, GridCoordinate coordinate)
        {
            EnsureSettings();
            ValidateTarget(target);

            if (!GridMath.IsWithinBounds(settings, coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is outside the grid bounds.");
            }

            target.position = GridMath.GridToWorld(settings, coordinate);
        }

        public void PlaceAtIndex(Transform target, int index)
        {
            EnsureSettings();
            ValidateTarget(target);

            GridCoordinate coordinate = GridMath.IndexToCoordinate(settings, index);
            target.position = GridMath.GridToWorld(settings, coordinate);
        }

        public void PlaceBatch(Transform[] targets, GridCoordinate[] coordinates)
        {
            EnsureSettings();

            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            if (coordinates == null)
            {
                throw new ArgumentNullException(nameof(coordinates));
            }

            if (targets.Length != coordinates.Length)
            {
                throw new ArgumentException("Targets and coordinates arrays must have the same length.");
            }

            int count = targets.Length;
            for (int index = 0; index < count; index++)
            {
                Transform target = targets[index];
                if (target == null)
                {
                    continue;
                }

                GridCoordinate coordinate = coordinates[index];
                if (!GridMath.IsWithinBounds(settings, coordinate))
                {
                    throw new ArgumentOutOfRangeException(nameof(coordinates), coordinate, "A coordinate is outside the grid bounds.");
                }

                target.position = GridMath.GridToWorld(settings, coordinate);
            }
        }

        public void PlaceInTraversalOrder(Transform[] cards)
        {
            EnsureSettings();

            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            int cardCount = cards.Length;
            int cellCount = settings.CellCount;
            if (cardCount > cellCount)
            {
                throw new ArgumentException(
                    "Card count exceeds grid capacity. Cards: " + cardCount + ", Cells: " + cellCount + ".",
                    nameof(cards));
            }

            for (int index = 0; index < cardCount; index++)
            {
                Transform card = cards[index];
                if (card == null)
                {
                    continue;
                }

                GridCoordinate coordinate = GridMath.IndexToCoordinate(settings, index);
                card.position = GridMath.GridToWorld(settings, coordinate);
            }
        }

        public bool TryGetCellFromRay(Ray ray, float maxDistance, out GridCoordinate coordinate, out Vector3 hitPoint)
        {
            EnsureSettings();

            bool hasHit;
            RaycastHit raycastHit;

            float castRadius = Mathf.Max(0f, settings.SelectionCastRadius);
            if (castRadius > 0f)
            {
                hasHit = Physics.SphereCast(ray, castRadius, out raycastHit, maxDistance);
            }
            else
            {
                hasHit = Physics.Raycast(ray, out raycastHit, maxDistance);
            }

            if (!hasHit)
            {
                coordinate = default;
                hitPoint = default;
                return false;
            }

            hitPoint = raycastHit.point;
            return TryGetCellFromWorld(hitPoint, out coordinate);
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            if (settings == null)
            {
                return;
            }

            settings.Validate();
            Vector3 effectiveCellSize = GridMath.GetEffectiveCellSize(settings);
            Vector3 wireSize = effectiveCellSize;

            int width = settings.Width;
            int height = settings.Height;

            Gizmos.color = gizmoCellColor;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 cellCenter = GridMath.GridToWorld(settings, new GridCoordinate(x, y));
                    Gizmos.DrawWireCube(cellCenter, wireSize);
                }
            }

            Gizmos.color = gizmoOriginColor;
            float planarSize = settings.Plane == GridPlane.XZ
                ? Mathf.Min(effectiveCellSize.x, effectiveCellSize.z)
                : Mathf.Min(effectiveCellSize.x, effectiveCellSize.y);
            float markerRadius = Mathf.Max(0.05f, planarSize * 0.1f);
            Gizmos.DrawSphere(settings.Origin, markerRadius);
        }
        #endregion

        #region Helpers
        private void EnsureSettings()
        {
            if (settings == null)
            {
                settings = new GridSettings();
            }
        }

        private static void ValidateTarget(Transform target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
        }
        #endregion
    }
}
