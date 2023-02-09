using Rhino.Geometry;
using System;

namespace DetectLinesInPicture {
    // Struktura s dvěmi celými čísly
    struct DInt : ICloneable{
        public int X, Y;

        public DInt(int x, int y) {
            X=x;
            Y=y;
        }

        // Následuje nedůležité
        public object Clone() => MemberwiseClone();

        public override bool Equals(object obj) => obj is DInt o && X == o.X && Y == o.Y;

        public override int GetHashCode() {
            int hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }

        public Point3d ToPoint2d() => new Point3d(X,Y,0);
    }

    // Třída s dvěmi celými čísly a jednou hodnotou značící uzavřenost
    internal class DIntB : ICloneable{
        public int X, Y;
        public bool N;

        public DIntB(int x, int y, bool n) {
            X=x;
            Y=y;
            N=n;
        }

        // Nastav hodnotu N
        internal void SetN(bool value){ N=value; }
                
        // Následuje nedůležité...
        public static bool operator ==(DIntB a, DIntB b) => a.Equals(b);

        public static bool operator !=(DIntB a, DIntB b) => !a.Equals(b);

        public object Clone() => MemberwiseClone();

        internal Point3d ToPoint2d() => new Point3d(X,Y,0);

        public override bool Equals(object obj){ 
            if (obj is DIntB d) { 
                if (X!=d.X) return false; 
                if (Y!=d.Y) return false; 
                if (N!=d.N) return false; 
                return true;
            }
            return false;
        }

        public override int GetHashCode() {
            int hashCode = -643396196;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + N.GetHashCode();
            return hashCode;
        }
    }

    // Struktura se třemi celými čísly, N je číslo pro hodnotu barvy
    struct TInt {
        public int X, Y;
        public int N;
        public TInt(int x, int y, int z) {
            X=x;
            Y=y;
            N=z;
        }
    }
}