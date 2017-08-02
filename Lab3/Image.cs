using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Lab3
{
    public class Image
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Height { get; private set; }
        public double Width { get; private set; }
        public List<Figure<Point2D>> Figures;
        public Border CanvasImage { get; private set; }

        public void MoveFigures(double x, double y)
        {
            double[] delta = { x, y };
            for (int i = 0; i < Figures.Count; i++)
            {
                Figures[i].Move(delta);
            }
        }
        public void MoveImage(double x, double y)
        {
            X += x;
            Y += y;
        }
        public bool Add(Figure<Point2D> f)
        {
            //for (int i = 1; i <= f.PointsCount; i++)
            //{
            //    if (f[i][0] < X || f[i][1] < Y || f[i][0] >= Width || f[i][1] >= Height)
            //        return false;
            //}
            Figures.Add(f);
            return true;
        }
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append(GetType().ToString() + ":\n");
            for (int i = 0; i < Figures.Count; i++)
                str.Append(Figures.ToString() + "\n");
            return str.ToString();
        }
        public void Scale(double k)
        {
            if (k <= 0)
                throw new ArgumentException("Factor must be positive");
            X *= k;
            Y *= k;
            Width *= k;
            Height *= k;
            Point2D imageCenter = new Point2D(X + Width / 2, Y + Height / 2);
            Point2D newCenter;
            for (int i = 0; i < Figures.Count; i++)
            {
                newCenter = new Point2D((Figures[i].Center[0] - imageCenter.X) * k + imageCenter.X,
                    (Figures[i].Center[1] - imageCenter.Y) * k + imageCenter.Y);
                Figures[i].MoveTo(newCenter);
                Figures[i].Scale(k);
            }
        }

        public Image(double x, double y, double width, double height, Border image, int n = 8)
        {
            if (width <= 0)
                throw new ArgumentException("Impossible width.");
            if (height <= 0)
                throw new ArgumentException("Impossible height.");
            X = x;
            Y = y;
            Width = width;
            Height = height;
            if (n <= 0)
                throw new ArgumentException(n + " is not appropriate quantity of figures.");
            else
                Figures = new List<Figure<Point2D>>(n);
            if (image != null)
                CanvasImage = image;
            else
                throw new ArgumentNullException("Canvas image can't be equal to null.");
        }
        public Image(double[,] coords, Border image, int n = 8)
        {
            if (coords.GetLength(0) < 2 || coords.GetLength(1) < 2)
                throw new ArgumentException("Not enough coordinates to initialize.");
            if (coords[1, 0] <= 0)
                throw new ArgumentException("Impossible width.");
            if (coords[1, 1] <= 0)
                throw new ArgumentException("Impossible height.");
            X = coords[0, 0];
            Y = coords[0, 1];
            Width = coords[1, 0];
            Height = coords[1, 1];
            if (n <= 0)
                throw new ArgumentException(n + " is not appropriate quantity of figures.");
            else
                Figures = new List<Figure<Point2D>>(n);
            if (image != null)
                CanvasImage = image;
            else
                throw new ArgumentNullException("Canvas image can't be equal to null.");
        }
        //public static Image Merge(Image i1, Image i2)
        //{
        //    double maxWidth = Math.Max(i1.Width, i2.Width);
        //    double maxHeight = Math.Max(i1.Height, i2.Height);
        //    double minX = Math.Min(i1.X, i2.X);
        //    double minY = Math.Min(i1.Y, i2.Y);
        //    Image newImage = new Image(minX, minY, maxWidth, maxHeight);
        //    newImage.figures = i1.figures;
        //    newImage.figures.AddRange(i2.figures);
        //    return newImage;
        //}
        // Draw, Save/Load
    }
}
