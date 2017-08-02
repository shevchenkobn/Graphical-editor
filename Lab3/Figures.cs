using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Lab3
{
    public class PointND
    {
        protected double[] coords;
        public int Dimension { get { return coords.Length; } }
        public override string ToString()
        {
            StringBuilder str = new StringBuilder().Append("(");
            for (int i = 0; i < coords.Length; i++)
                str.Append(coords[i]).Append(", ");
            return GetType().ToString() + ": " + str.Append(")").ToString();
        }
        public double this[int i]
        {
            get { return coords[i]; }
            internal protected set { coords[i] = value; }
        }
        public double[] ToArray()
        {
            return coords;
        }
        public PointND(double[] coords)
        {
            this.coords = coords;
        }
        protected PointND()
        { }
        public PointND(PointND coords, int n)
        {
            this.coords = new double[n];
            if (n < coords.Dimension)
            {
                for (int i = 0; i < n; i++)
                {
                    this.coords[i] = coords[i];
                }
            }
            else
            {
                for (int i = 0; i < coords.Dimension; i++)
                {
                    this.coords[i] = coords[i];
                }
            }
        }
        static public double SegmentLength(PointND p1, PointND p2)
        {
            double distSqr = 0;
            if (p1.Dimension >= p2.Dimension)
            {
                for (int i = 0; i < p2.Dimension; i++)
                {
                    distSqr += Math.Pow(p1[i] + p2[i], 2);
                }
                for (int i = p2.Dimension; i < p1.Dimension; i++)
                    distSqr += Math.Pow(p1[i], 2);
            }
            else
            {
                for (int i = 0; i < p1.Dimension; i++)
                {
                    distSqr += Math.Pow(p1[i] + p2[i], 2);
                }
                for (int i = p1.Dimension; i < p2.Dimension; i++)
                    distSqr += Math.Pow(p2[i], 2);
            }
            return Math.Sqrt(distSqr);
        }
        public static PointND operator -(PointND p1, PointND p2)
        {
            double[] pCoords = p1.ToArray();
            if (p1.Dimension >= p2.Dimension)
            {
                pCoords = p1.ToArray();
                for (int i = 0; i < pCoords.Length; i++)
                {
                    pCoords[i] -= p2.Dimension > i ? p2[i] : 0;
                }
            }
            else
            {
                pCoords = p2.ToArray();
                for (int i = 0; i < pCoords.Length; i++)
                {
                    pCoords[i] -= p1.Dimension > i ? p1[i] : 0;
                }
            }
            return new PointND(pCoords);
        }
    }
    public class Point2D : PointND
    {
        public double X { get { return coords[0]; } set { coords[0] = value; } }
        public double Y { get { return coords[1]; } set { coords[1] = value; } }
        public Point2D(double x, double y)
        {
            coords = new double[2];
            X = x;
            Y = y;
        }
        public Point2D(double[] coords)
        {
            if (coords == null || coords.Length < 2)
                throw new ArgumentException("Not enough coordinates for point.");
            this.coords = new double[2];
            if (coords == null)
            {
                X = Y = 0;
            }
            else
            {
                X = coords[0];
                Y = coords[1];
            }
        }
    }
    public class Point3D : PointND
    {
        public double X { get { return coords[0]; } set { coords[0] = value; } }
        public double Y { get { return coords[1]; } set { coords[1] = value; } }
        public double Z { get { return coords[2]; } set { coords[2] = value; } }
        public Point3D(double x, double y, double z)
        {
            if (coords.Length < 3)
                throw new ArgumentException("Not enough coordinates for point.");
            coords = new double[3];
            X = x;
            Y = y;
            Z = z;
        }
        public Point3D(double[] coords = null)
        {
            if (coords == null || coords.Length < 3)
                throw new ArgumentException("Not enough coordinates for point.");
            this.coords = new double[3];
            if (coords == null)
            {
                X = Y = Z = 0;
            }
            else
            {
                X = coords[0];
                Y = coords[1];
                Z = coords[2];
            }
        }
    }
    public abstract class Figure<Point> where Point:PointND
    {
        public Point Center { get { return points[0]; } set { points[0] = value; } }
        protected Point[] points { get; set; }
        public int PointsCount { get { return points.Length - 1; } }
        public Shape CanvasImage { get; set; }
        virtual public Point this[int i]
        {
            get { return points[i]; }
            protected set { points[i] = value; }
        }
        virtual public void Move(Point delta)
        {
            for (int i = 0; i < points.Length; i++)
            {
                for (int j = 0; j < points[i].Dimension; j++)
                {
                    if (delta.Dimension > j)
                        points[i][j] += delta[j];
                }
            }
        }
        virtual public void Move(double[] delta)
        {
            for (int i = 0; i < points.Length; i++)
            {
                for (int j = 0; j < points[i].Dimension; j++)
                {
                    if (delta.Length > j)
                        points[i][j] += delta[j];
                }
            }
        }
        virtual public void MoveTo(Point newCenter)
        {
            for (int i = 1; i < points.Length; i++)
            {
                for (int j = 0; j <= points[i].Dimension; j++)
                {
                    points[i][j] += newCenter[j] - points[0][j];
                }
            }
        }
        virtual public void Scale(double k)
        {
            if (k <= 0)
                throw new ArgumentException("Factor must be positive");
            for (int i = 1; i < points.Length; i++)
            {
                for (int j = 0; j < points[i].Dimension; j++)
                {
                    points[i][j] += (points[i][j] - points[0][j]) * k + points[0][j];
                }
            }
        }
        virtual public double Perimeter()
        {
            double sum = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                sum += PointND.SegmentLength(points[i], points[i + 1]);
            }
            return sum;
        }
        virtual public PointND[] ToNewSpace(int n)
        {
            if (n <= 0)
                throw new ArgumentOutOfRangeException("Dimension must be positive.");
            if (n == points[0].Dimension)
                return points;
            PointND[] newPoints = new PointND[points.Length];
            if (n < points[0].Dimension)
            {
                for (int i = 0; i < newPoints.Length; i++)
                {
                    double[] temp = new double[n];
                    for (int j = 0; j < n; j++)
                    {
                        temp[j] = points[i][j];
                    }
                    newPoints[i] = new PointND(temp);
                }
            }
            if (n > points[0].Dimension)
            {
                for (int i = 0; i < newPoints.Length; i++)
                {
                    double[] temp = new double[n];
                    for (int j = 0; j < points[0].Dimension; j++)
                    {
                        temp[j] = points[i][j];
                    }
                    for (int j = points[0].Dimension; j < n; j++)
                    {
                        temp[j] = 0;
                    }
                    newPoints[i] = new PointND(temp);
                }
            }
            return newPoints;
        }
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < points.Length; i++)
                str.Append(points[i]).Append("; ");
            return GetType().ToString() + ": " + str.ToString();
        }
        public Figure(Point[] pts)
        {
            points = new Point[pts.Length + 1];
            for (int i = 1; i < points.Length; i++)
            {
                points[i] = pts[i - 1];
            }
        }
        public Figure()
        { }
        public virtual double[][] ToArray()
        {
            double[][] coords = new double[PointsCount][];
            for (int i = 0; i < points.Length; i++)
                coords[i] = points[i].ToArray();
            return coords;
        }
    }
    
    /**
     * The following class is made abstract because calculation of
     * arbitrary polygon circumcenter and its square is quite difficult.
     **/
    public abstract class Polygon_ : Figure<Point2D>
    {
        abstract public double Square();
        override public Point2D this[int i]
        {
            get
            {
                if (i > 0)
                    return points[i];
                else
                    throw new IndexOutOfRangeException();
            }
            protected set
            {
                if (i > 0)
                    points[i] = value;
                else
                    throw new IndexOutOfRangeException();
            }
        }
        public override double Perimeter()
        {
            return base.Perimeter()
                + Point2D.SegmentLength(points.Last(), points[0]);
        }
        public Polygon_(Point2D[] points) : base(points)
        {
        }
    }
    public class Triangle_ : Polygon_
    {
        public double X1 { get { return points[1].X; } }
        public double Y1 { get { return points[1].Y; } }
        public double X2 { get { return points[2].X; } }
        public double Y2 { get { return points[2].Y; } }
        public double X3 { get { return points[3].X; } }
        public double Y3 { get { return points[3].Y; } }
        public Triangle_(Point2D[] points) : base(points)
        {
            double[] edges = sortedEdges(points);
            if (edges[2] > edges[0] + edges[1])
                throw new ArgumentException("Points are lying on one line.");

            // Finding a triangle circumcenter
            double a, bx, by;
            a = X2 * Y3 - Y2 * X3
                - X1 * Y3 + Y1 * X3
                + X1 * Y2 - Y1 * X2;
            bx = -((X2 * X2 + Y2 * Y2) * Y3 - (X3 * X3 + Y3 * Y3) * Y2
                - (X1 * X1 + Y1 * Y1) * Y3 + (X3 * X3 + Y3 * Y3) * Y1
                + (X1 * X1 + Y1 * Y1) * Y2 - (X2 * X2 + Y2 * Y2) * Y1);
            by = (X2 * X2 + Y2 * Y2) * X3 - (X3 * X3 + Y3 * Y3) * X2
                - (X1 * X1 + Y1 * Y1) * X3 + (X3 * X3 + Y3 * Y3) * X1
                + (X1 * X1 + Y1 * Y1) * X2 - (X2 * X2 + Y2 * Y2) * X1;
            Center = new Point2D(-bx / 2 * a, -by / 2 * a);
        }
        public override double Square()
        {
            double sinA = Math.Sin(Math.Abs(Math.Atan2(X2 - X1, Y2 - Y1) - Math.Atan2(X3 - X1, Y3 - Y1)));
            double AB = PointND.SegmentLength(points[2], points[1]);
            double AC = Point2D.SegmentLength(points[3], points[1]);
            return AB * AC * sinA;
        }
        public static Point2D[] ToPointsArray(double[][] coords)
        {
            Point2D[] points = new Point2D[coords.Length];
            for (int i = 0; i < coords.Length; i++)
                points[i] = new Point2D(coords[i]);
            return points;
        }
        static protected double[] sortedEdges(Point2D[] points)
        {
            if (points.Length != 3)
                throw new ArgumentException("Triangle must be initialized with 3 points.");

            double[] edges = new double[3];
            for (int i = 0; i < edges.Length; i++)
            {
                edges[i] = Point2D.SegmentLength(points[i], points[i < edges.Length ? i : 0]);
            }
            Array.Sort(edges, (a, b) => {
                if (a > b)
                    return -1;
                else if (a < b)
                    return 1;
                else return 0;
            });
            return edges;
        }
    }
    public class RegularTriangle : Triangle_
    {
        public RegularTriangle(Point2D[] points) : base(points)
        {
            if (PointND.SegmentLength(points[1], points[2]) != Point2D.SegmentLength(points[2], points[3])
                || Point2D.SegmentLength(points[1], points[2]) != Point2D.SegmentLength(points[1], points[3])
                || Point2D.SegmentLength(points[1], points[3]) != Point2D.SegmentLength(points[2], points[3]))
                throw new ArgumentException("All edges must be equal.");
        }
    }
    public class RectangularTriangle : Triangle_
    {
        public RectangularTriangle(Point2D[] points) : base(points)
        {
            double[] edges = sortedEdges(points);
            if (Math.Pow(edges[2], 2) != Math.Pow(edges[0], 2) + Math.Pow(edges[1], 2))
                throw new ArgumentException("Given triangle is not rectangular.");
        }
    }
    public class FilledTriangle : Triangle_
    {
        public Color Filling { get; set; }
        public FilledTriangle(Point2D[] points) : base(points)
        {

        }
    }
    
    public class Tetrahedron : Figure<Point3D>
    {
        public Tetrahedron(Point3D[] points)
        {
            this.points = points;
            // TODO: find circumcenter
        }
    }
}
