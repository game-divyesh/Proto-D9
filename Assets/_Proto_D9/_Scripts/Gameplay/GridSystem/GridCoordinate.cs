using System;

namespace MatchingPair.Gameplay.GridSystem
{
    [Serializable]
    public struct GridCoordinate : IEquatable<GridCoordinate>
    {
        #region Fields
        public int X;
        public int Y;
        #endregion

        #region Constructors
        public GridCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }
        #endregion

        #region Methods
        public bool Equals(GridCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }
        #endregion

        #region Operators
        public static bool operator ==(GridCoordinate left, GridCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridCoordinate left, GridCoordinate right)
        {
            return !left.Equals(right);
        }
        #endregion
    }
}
