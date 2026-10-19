using System;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Mathematical utilities replacing native DceMath exports.
    /// </summary>
    public static class MathUtils
    {
        public const double DefaultTolerance = 1e-9;

        public static bool IsZero(double val, double tol = DefaultTolerance)
        {
            return Math.Abs(val) <= tol;
        }

        public static bool AreCollinear(Point2D a, Point2D b, Point2D c, double tol = DefaultTolerance)
        {
            double cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            return Math.Abs(cross) <= tol;
        }

        public static double DegToRad(double deg) => deg * Math.PI / 180.0;
        public static double RadToDeg(double rad) => rad * 180.0 / Math.PI;

        public static bool Solve2x2(
            double a1, double b1, double c1,
            double a2, double b2, double c2,
            out double x, out double y,
            double tol = DefaultTolerance)
        {
            x = 0.0;
            y = 0.0;
            double det = a1 * b2 - a2 * b1;
            if (Math.Abs(det) <= tol)
            {
                return false;
            }
            x = (c1 * b2 - c2 * b1) / det;
            y = (a1 * c2 - a2 * c1) / det;
            return true;
        }

        public static int SolveQuadratic(double a, double b, double c, out double? root1, out double? root2)
        {
            root1 = null;
            root2 = null;

            if (Math.Abs(a) < 1e-12)
            {
                if (Math.Abs(b) < 1e-12)
                {
                    return 0;
                }
                root1 = -c / b;
                return 1;
            }

            double discriminant = b * b - 4.0 * a * c;
            if (discriminant < -1e-9)
            {
                return 0;
            }
            if (Math.Abs(discriminant) <= 1e-9)
            {
                root1 = -b / (2.0 * a);
                return 1;
            }

            double sqrtD = Math.Sqrt(discriminant);
            root1 = (-b - sqrtD) / (2.0 * a);
            root2 = (-b + sqrtD) / (2.0 * a);
            return 2;
        }

        public static double LinearInterpolate(double[] x, double[] y, double x0)
        {
            if (x == null || y == null || x.Length != y.Length || x.Length == 0)
            {
                throw new ArgumentException("Valid x and y arrays of equal length are required.");
            }
            if (x.Length == 1 || x0 <= x[0]) return y[0];
            if (x0 >= x[x.Length - 1]) return y[y.Length - 1];

            for (int i = 0; i < x.Length - 1; i++)
            {
                if (x0 >= x[i] && x0 <= x[i + 1])
                {
                    double dx = x[i + 1] - x[i];
                    if (Math.Abs(dx) < 1e-12) return y[i];
                    double t = (x0 - x[i]) / dx;
                    return y[i] + t * (y[i + 1] - y[i]);
                }
            }
            return y[y.Length - 1];
        }

        public static double BilinearInterpolate(double[,] grid, double[] xArr, double[] yArr, double x, double y)
        {
            if (grid == null || xArr == null || yArr == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }
            int nx = xArr.Length;
            int ny = yArr.Length;
            int ix = Math.Max(0, Math.Min(nx - 2, FindInterval(xArr, x)));
            int iy = Math.Max(0, Math.Min(ny - 2, FindInterval(yArr, y)));

            double x0 = xArr[ix], x1 = xArr[ix + 1];
            double y0 = yArr[iy], y1 = yArr[iy + 1];
            double tx = Math.Abs(x1 - x0) < 1e-12 ? 0.0 : (x - x0) / (x1 - x0);
            double ty = Math.Abs(y1 - y0) < 1e-12 ? 0.0 : (y - y0) / (y1 - y0);

            double top = grid[ix, iy] * (1 - tx) + grid[ix + 1, iy] * tx;
            double bot = grid[ix, iy + 1] * (1 - tx) + grid[ix + 1, iy + 1] * tx;
            return top * (1 - ty) + bot * ty;
        }

        private static int FindInterval(double[] arr, double val)
        {
            for (int i = 0; i < arr.Length - 1; i++)
            {
                if (val <= arr[i + 1]) return i;
            }
            return arr.Length - 2;
        }

        public static bool IsClockwise(Point2D a, Point2D b, Point2D c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) < 0.0;
        }

        public static double Ratio(double a, double b)
        {
            return Math.Abs(b) < 1e-12 ? 0.0 : a / b;
        }

        public static double RatioPercent(double a, double b)
        {
            return Ratio(a, b) * 100.0;
        }

        public static double HalfPerimeter(double a, double b, double c)
        {
            return (a + b + c) * 0.5;
        }
    }
}
