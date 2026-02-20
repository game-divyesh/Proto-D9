using System;
using UnityEngine;

namespace MatchingPair.Gameplay.GridSystem
{
    public static class GridMath
    {
        private const float MinCellAxisSize = 0.01f;
        private const float MinStepAxis = 0.0001f;

        #region Sizing
        public static Vector3 GetEffectiveCellSize(GridSettings settings)
        {
            ValidateSettingsReference(settings);

            Vector3 baseCellSize = settings.CellSize;
            baseCellSize.x = Mathf.Max(MinCellAxisSize, baseCellSize.x);
            baseCellSize.y = Mathf.Max(MinCellAxisSize, baseCellSize.y);
            baseCellSize.z = Mathf.Max(MinCellAxisSize, baseCellSize.z);
            return baseCellSize;
        }

        public static Vector2 GetCellStep(GridSettings settings)
        {
            ValidateSettingsReference(settings);

            Vector3 effectiveCellSize = GetEffectiveCellSize(settings);
            float spacing = Mathf.Max(0f, settings.Spacing);

            float stepX = effectiveCellSize.x + spacing;
            float stepY = settings.Plane == GridPlane.XZ
                ? effectiveCellSize.z + spacing
                : effectiveCellSize.y + spacing;

            stepX = Mathf.Max(MinStepAxis, stepX);
            stepY = Mathf.Max(MinStepAxis, stepY);
            return new Vector2(stepX, stepY);
        }
        #endregion

        #region Positioning
        public static Vector2 CalculateGridStartOffset(GridSettings settings)
        {
            ValidateSettingsReference(settings);

            Vector2 step = GetCellStep(settings);
            float widthSpan = (Mathf.Max(1, settings.Width) - 1) * step.x;
            float heightSpan = (Mathf.Max(1, settings.Height) - 1) * step.y;

            float startX = -0.5f * widthSpan;
            float startY = 0.5f * heightSpan;
            return new Vector2(startX, startY);
        }

        public static Vector3 GridToWorld(GridSettings settings, GridCoordinate coordinate)
        {
            ValidateSettingsReference(settings);

            Vector2 step = GetCellStep(settings);
            Vector2 startOffset = CalculateGridStartOffset(settings);

            float localX = startOffset.x + coordinate.X * step.x;
            float localY = startOffset.y - coordinate.Y * step.y;

            Vector3 origin = settings.Origin;
            if (settings.Plane == GridPlane.XZ)
            {
                return new Vector3(origin.x + localX, origin.y, origin.z + localY);
            }

            return new Vector3(origin.x + localX, origin.y + localY, origin.z);
        }

        public static GridCoordinate WorldToGrid(GridSettings settings, Vector3 worldPosition, bool clampToBounds)
        {
            ValidateSettingsReference(settings);

            Vector2 step = GetCellStep(settings);
            Vector2 startOffset = CalculateGridStartOffset(settings);

            float localX = worldPosition.x - settings.Origin.x;
            float localY = settings.Plane == GridPlane.XZ
                ? worldPosition.z - settings.Origin.z
                : worldPosition.y - settings.Origin.y;

            int gridX = Mathf.RoundToInt((localX - startOffset.x) / step.x);
            int gridY = Mathf.RoundToInt((startOffset.y - localY) / step.y);

            GridCoordinate coordinate = new GridCoordinate(gridX, gridY);
            if (!clampToBounds)
            {
                return coordinate;
            }

            return ClampToBounds(settings, coordinate);
        }
        #endregion

        #region CoordinatesAndIndices
        public static int CoordinateToIndex(GridSettings settings, GridCoordinate coordinate)
        {
            ValidateSettingsReference(settings);
            ValidateCoordinate(settings, coordinate);

            int width = settings.Width;
            int height = settings.Height;
            int rowIndex;
            int columnIndex;

            switch (settings.TraversalOrder)
            {
                case TraversalOrder.LeftToRight_TopToBottom:
                    rowIndex = coordinate.Y;
                    columnIndex = coordinate.X;
                    break;

                case TraversalOrder.LeftToRight_BottomToTop:
                    rowIndex = (height - 1) - coordinate.Y;
                    columnIndex = coordinate.X;
                    break;

                case TraversalOrder.RightToLeft_TopToBottom:
                    rowIndex = coordinate.Y;
                    columnIndex = (width - 1) - coordinate.X;
                    break;

                case TraversalOrder.RightToLeft_BottomToTop:
                    rowIndex = (height - 1) - coordinate.Y;
                    columnIndex = (width - 1) - coordinate.X;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return rowIndex * width + columnIndex;
        }

        public static GridCoordinate IndexToCoordinate(GridSettings settings, int index)
        {
            ValidateSettingsReference(settings);

            int cellCount = settings.CellCount;
            if (index < 0 || index >= cellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is outside the grid bounds.");
            }

            int width = settings.Width;
            int height = settings.Height;
            int rowIndex = index / width;
            int columnIndex = index % width;
            int x;
            int y;

            switch (settings.TraversalOrder)
            {
                case TraversalOrder.LeftToRight_TopToBottom:
                    x = columnIndex;
                    y = rowIndex;
                    break;

                case TraversalOrder.LeftToRight_BottomToTop:
                    x = columnIndex;
                    y = (height - 1) - rowIndex;
                    break;

                case TraversalOrder.RightToLeft_TopToBottom:
                    x = (width - 1) - columnIndex;
                    y = rowIndex;
                    break;

                case TraversalOrder.RightToLeft_BottomToTop:
                    x = (width - 1) - columnIndex;
                    y = (height - 1) - rowIndex;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new GridCoordinate(x, y);
        }

        public static bool IsWithinBounds(GridSettings settings, GridCoordinate coordinate)
        {
            ValidateSettingsReference(settings);

            return coordinate.X >= 0 &&
                   coordinate.Y >= 0 &&
                   coordinate.X < settings.Width &&
                   coordinate.Y < settings.Height;
        }

        public static GridCoordinate ClampToBounds(GridSettings settings, GridCoordinate coordinate)
        {
            ValidateSettingsReference(settings);

            int clampedX = Mathf.Clamp(coordinate.X, 0, settings.Width - 1);
            int clampedY = Mathf.Clamp(coordinate.Y, 0, settings.Height - 1);
            return new GridCoordinate(clampedX, clampedY);
        }
        #endregion

        #region Validation
        private static void ValidateSettingsReference(GridSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
        }

        private static void ValidateCoordinate(GridSettings settings, GridCoordinate coordinate)
        {
            if (!IsWithinBounds(settings, coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is outside the grid bounds.");
            }
        }
        #endregion
    }
}
