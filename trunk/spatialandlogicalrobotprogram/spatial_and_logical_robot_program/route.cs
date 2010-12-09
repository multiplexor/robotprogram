using System;
using System.Drawing;

namespace spatial_and_logical_robot_program
{
    [Serializable]
    class Route
    {
        public Point[] Route1;
        internal int Iterator1;
        internal int Iterator2;

        public Route()
        {
            Route1 = new Point[100];
        }

        public void PointAdd(Point point)
        {
            Route1[Iterator1] = point;
            Iterator1++;
        }

        public Point GetNewDestinationPoint()
        {
            if (Iterator2 > Iterator1)
            {
                return Route1[Iterator2 - 1];
            }
            Iterator2++;
            return Route1[Iterator2-1];
        }
    }
}
