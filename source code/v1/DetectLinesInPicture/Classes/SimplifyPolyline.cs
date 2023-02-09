using Rhino.Commands;
using System.Collections.Generic;


namespace DetectLinesInPicture {
    internal static class SimplifyPolyline {
        // https://github.com/BobLd/RamerDouglasPeuckerNet
        // Ramer-Douglas-Peucker algorithm for 2D data in C#.
     
        /// <summary>
        /// Uses the Ramer Douglas Peucker algorithm to reduce the number of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns></returns>
        public static DIntB[] Reduce(DIntB[] points, double tolerance) {
            if (points == null || points.Length < 4) return points;

            if (double.IsInfinity(tolerance) || double.IsNaN(tolerance)) return points;
            tolerance *= tolerance;
            if (tolerance <= float.Epsilon) return points;

            int firstIndex = 0;
            int lastIndex = points.Length - 1;
            List<int> indexesToKeep = new List<int>();

            // Add the first and last index to the keepers
            indexesToKeep.Add(firstIndex);

            // Body připojující se doprostřed těla polyline
            for (int i=1; i<points.Length-1; i++) {
                if (points[i].N) indexesToKeep.Add(i);
            }

            indexesToKeep.Add(lastIndex);

            // The first and the last point cannot be the same
            while (points[firstIndex].Equals(points[lastIndex])) {
                lastIndex--;
            }

            Reduce(points, firstIndex, lastIndex, tolerance, ref indexesToKeep);

            int l = indexesToKeep.Count;
            DIntB[] returnPoints = new DIntB[l];
            indexesToKeep.Sort();

            for (int i=0; i<l; i++) returnPoints[i] = points[indexesToKeep[i]];

            return returnPoints;
        }

        /// <summary>
        /// Douglases the peucker reduction.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="firstIndex">The first point's index.</param>
        /// <param name="lastIndex">The last point's index.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="indexesToKeep">The points' index to keep.</param>
        private static void Reduce(DIntB[] points, int firstIndex, int lastIndex, double tolerance, ref List<int> indexesToKeep) {
            double maxDistance = 0;
            int indexFarthest = 0;

            DIntB point1 = points[firstIndex];
            DIntB point2 = points[lastIndex];
                  
            double distXY = point1.X * point2.Y - point2.X * point1.Y;
            double distX = point2.X - point1.X;
            double distY = point1.Y - point2.Y;
            double bottom = distX * distX + distY * distY;

            for (int i = firstIndex; i < lastIndex; i++) {
                // Perpendicular Distance
                DIntB point = points[i];
                double area = distXY + distX * point.Y + distY * point.X;
                double distance = (area / bottom) * area;

                if (distance > maxDistance) {
                    maxDistance = distance;
                    indexFarthest = i;
                }
            }

            if (maxDistance > tolerance) { // && indexFarthest != 0)
                //Add the largest point that exceeds the tolerance
                indexesToKeep.Add(indexFarthest);
                Reduce(points, firstIndex, indexFarthest, tolerance, ref indexesToKeep);
                Reduce(points, indexFarthest, lastIndex, tolerance, ref indexesToKeep);
            }
        }
    }
}