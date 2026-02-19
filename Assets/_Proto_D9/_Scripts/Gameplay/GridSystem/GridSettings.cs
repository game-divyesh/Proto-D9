using System;
using UnityEngine;

namespace MatchingPair.Gameplay.GridSystem
{
    [Serializable]
    public class GridSettings
    {
        #region Dimensions
        [Min(1)] public int Width = 2;
        [Min(1)] public int Height = 2;
        #endregion

        #region Cell
        public Vector3 CellSize = new Vector3(1f, 1f, 1f);
        [Min(0f)] public float Spacing = 0.1f;
        public GridPlane Plane = GridPlane.XY;
        public TraversalOrder TraversalOrder = TraversalOrder.LeftToRight_TopToBottom;
        public Vector3 Origin = Vector3.zero;
        #endregion

        #region AutoFit
        public bool AutoFitToArea = false;
        public Vector2 TargetWorldSize = new Vector2(5f, 5f);
        #endregion

        #region Selection
        [Min(0f)] public float SelectionCastRadius = 0f;
        #endregion

        #region Properties
        public int CellCount => Width * Height;
        #endregion

        #region Methods
        public void Validate()
        {
            Width = Mathf.Max(1, Width);
            Height = Mathf.Max(1, Height);
            CellSize.x = Mathf.Max(0.01f, CellSize.x);
            CellSize.y = Mathf.Max(0.01f, CellSize.y);
            CellSize.z = Mathf.Max(0.01f, CellSize.z);
            Spacing = Mathf.Max(0f, Spacing);
            SelectionCastRadius = Mathf.Max(0f, SelectionCastRadius);
            TargetWorldSize.x = Mathf.Max(0f, TargetWorldSize.x);
            TargetWorldSize.y = Mathf.Max(0f, TargetWorldSize.y);
        }
        #endregion
    }
}
