using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;

namespace Lab3
{
    /**
     * 
     * This file is for state classes declaration
     *
     **/
    partial class DrawingManager
    {
        delegate void EditorAction(Point mousePosition);
        abstract class EditorState
        {
            protected IChangeDrawingState _subject;
            protected Dictionary<int, EditorAction> _startActions;
            protected Dictionary<int, EditorAction> _continueActions;
            protected Dictionary<int, EditorAction> _finishActions;
            protected bool _captureMouse;
            public bool CaptureMouse { get => _captureMouse; }
            public EditorAction StartAction;
            public EditorAction ContinueAction;
            public EditorAction FinishAction;
            public EditorState(IChangeDrawingState subject)
            {
                _subject = subject;
                StartAction = new EditorAction((MouseDownPosition) => { });
                ContinueAction = new EditorAction((currentMousePosition) => { });
                FinishAction = new EditorAction((mouseUpPostion) => { });
                _subject.IsOperationInProcess = true;
                _captureMouse = true;
            }
            public virtual void Reset()
            {
                _subject.IsOperationInProcess = false;
            }
            public virtual Point GetMousePostition(MouseEventArgs e)
            {
                return e.GetPosition(_subject.CurrentImage);
            }
            protected double DistanceBetweenPoints(Point a, Point b)
            {
                return Math.Sqrt(SqrDistanceBetweenPoints(a, b));
            }
            protected double SqrDistanceBetweenPoints(Point a, Point b)
            {
                return Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2);
            }
        }

        // Stub state class also used to switch elements
        class SwitchElementState : EditorState
        {

            public SwitchElementState(DrawingManager subject) : base(subject)
            {
                _subject.IsOperationInProcess = false;
                _captureMouse = false;
                FinishAction = (mouseUpPosition) =>
                {
                    var images = _subject.Canvas.Children;
                    Border image = null;
                    for (int i = images.Count - 1; i >= 0; i--)
                    {
                        image = images[i] as Border;
                        if (image != null)
                        {
                            if (image.IsMouseOver)
                                    break;
                        }
                        image = null;
                    }
                    if (image != null)
                    {
                        _subject.CurrentImage = image;
                        var figures = (image.Child as Canvas).Children;
                        Shape figure = null;
                        for (int i = figures.Count - 1; i >= 0; i--)
                        {
                            figure = figures[i] as Shape;
                            if (figure != null)
                            {
                                if (figure.IsMouseOver)
                                    break;
                            }
                            figure = null;
                        }
                        if (figure != null)
                            _subject.CurrentFigure = figure;
                    }
                };
            }

            public override Point GetMousePostition(MouseEventArgs e)
            {
                return e.GetPosition(_subject.Canvas);
            }
        }

        // Drawing Image state class
        class ImageDrawingState : EditorState
        {
            Point _firstPoint;
            Border _image;
            public ImageDrawingState(IChangeDrawingState subject) : base(subject)
            {
                _firstPoint = new Point();

                StartAction = (Point mouseDownPosition) =>
                {
                    _firstPoint = mouseDownPosition;
                    _image = new Border() {
                        Height = 0,
                        Width = 0,
                        Background = Brushes.Transparent
                    };

                    Canvas.SetLeft(_image, mouseDownPosition.X);
                    Canvas.SetTop(_image, mouseDownPosition.Y);
                    _subject.Canvas.Children.Add(_image);
                    _subject.CurrentImage = _image;
                };

                ContinueAction = (Point currentCursorPosition) =>
                {
                    double width = currentCursorPosition.X - _firstPoint.X;
                    double height = currentCursorPosition.Y - _firstPoint.Y;
                    if (width >= 0)
                        _image.Width = width;
                    else
                    {
                        _image.Width = Math.Abs(width);
                        Canvas.SetLeft(_image, _firstPoint.X + width);
                    }
                    if (height >= 0)
                        _image.Height = height;
                    else
                    {
                        _image.Height = Math.Abs(height);
                        Canvas.SetTop(_image, _firstPoint.Y + height);
                    }
                };

                FinishAction = (mouseUpPosition) =>
                {
                    if (_image.Width == 0 || _image.Height == 0 ||
                            _image.BorderThickness.Left + _image.BorderThickness.Right >= _image.Width ||
                            _image.BorderThickness.Top + _image.BorderThickness.Bottom >= _image.Height)
                    {
                        _subject.Canvas.Children.Remove(_image);
                        _subject.CurrentImage = null;
                    }
                    else
                    {
                        //hm... maybe some other manipulations needed before these
                        //image.Focusable = true;
                        _image.Child = new Canvas()
                        {
                            //Background = preferences.ImageBackground,
                            Width = _image.Width - _image.BorderThickness.Left - _image.BorderThickness.Right,
                            Height = _image.Height - _image.BorderThickness.Top - _image.BorderThickness.Bottom,
                            Background = _subject.Canvas.Background,
                            ClipToBounds = true,
                            IsHitTestVisible = true
                        };
                        // We can focus this image or not
                        _subject.FireCurrentElementsEvents(CurrentElement.Image);
                    }
                    // Here we can either switch to UndefinedState or reset all the variables
                    //subject.SwitchState(EditorMode.Undefined);
                    // we reset variables
                    Reset();
                };
            }
            public override void Reset()
            {
                base.Reset();
                _image = null;
            }
            public override Point GetMousePostition(MouseEventArgs e)
            {
                return e.GetPosition(_subject.Canvas);
            }
        }

        /**
         * Drawing state classes below
         **/

        class TriangleDrawingState : EditorState
        {
            protected PointCollection _trianglePoints;
            public TriangleDrawingState(IChangeDrawingState subject) : base(subject)
            {
                _trianglePoints = new PointCollection();

                _startActions = new Dictionary<int, EditorAction>(2);

                _startActions[0] = (mouseDownPosition) =>
                {
                    var polyline = new Polyline();
                    (_subject.CurrentImage.Child as Canvas).Children.Add(polyline);
                    _subject.CurrentFigure = polyline;
                    _trianglePoints = new PointCollection();
                    _trianglePoints.Add(mouseDownPosition);
                    (_subject.CurrentFigure as Polyline).Points = _trianglePoints;
                };
                _startActions[2] = (mouseDownPosition) =>
                {
                    //var trianglePoints = (subject.CurrentFigure as Polyline).Points;
                    // hm... maybe some other manipulations needed before these
                    _trianglePoints.Add(mouseDownPosition);
                    var triangle = new Polygon()
                    {
                        //Stroke = preferences.FigureStroke,
                        //StrokeThickness = preferences.FigureStrokeThickness,
                        //Fill = preferences.FigureFilling,
                        Focusable = true
                    };
                    triangle.Points = _trianglePoints;
                    var image = _subject.CurrentImage.Child as Canvas;
                    Canvas.SetTop(triangle, 0);
                    Canvas.SetLeft(triangle, 0);
                    image.Children.Remove(_subject.CurrentFigure);
                    image.Children.Add(triangle);
                    _subject.CurrentFigure = triangle;
                    // We can focus this figure or not
                    //subject.CurrentFigure.Focus();
                };
                StartAction = (mouseDownPosition) =>
                {
                    if (_subject.CurrentImage == null)
                        return;
                    _startActions[_trianglePoints.Count](mouseDownPosition);
                };

                _continueActions = new Dictionary<int, EditorAction>(3);

                _continueActions[1] = (currentCursorPosition) =>
                {
                    (_subject.CurrentFigure as Polyline).Points.Add(currentCursorPosition);
                };
                _continueActions[2] = (currentCursorPosition) =>
                {
                    (_subject.CurrentFigure as Polyline).Points[1] = currentCursorPosition;
                };
                _continueActions[3] = (currentCursorPosition) =>
                {
                    (_subject.CurrentFigure as Polygon).Points[2] = currentCursorPosition;
                };
                ContinueAction = (currentCursorPosition) =>
                {
                    if (_subject.CurrentImage == null)
                        return;
                    _continueActions[_trianglePoints.Count](currentCursorPosition);
                };

                FinishAction = (mouseUpPosition) =>
                {
                    if (_subject.CurrentImage == null)
                        return;
                    if (_trianglePoints.Count == 3)
                    {
                         if (PositionOfPointYToLineBySegment(_trianglePoints[0], _trianglePoints[1], _trianglePoints[2]) == 0)
                         {
                             (_subject.CurrentImage.Child as Canvas).Children.Remove(_subject.CurrentFigure);
                             _subject.CurrentFigure = null;
                         }
                         Reset();
                    }
                };
            }

            public override void Reset()
            {
                base.Reset();
                if (_subject.CurrentFigure is Polyline)
                {
                    (_subject.CurrentImage.Child as Canvas).Children.Remove(_subject.CurrentFigure);
                    _subject.CurrentFigure = null;
                }
                _trianglePoints = new PointCollection();
            }
            protected Point MiddleOfSegment(Point a, Point b)
            {
                return new Point(a.X + (b.X - a.X) / 2, a.Y + (b.Y - a.Y) / 2);
            }
            protected int PositionOfPointYToLineBySegment(Point p1, Point p2, Point p)
            {
                if (p1.X == p2.X)
                {
                    if (p.X < p1.X)
                        return 1;
                    else if (p.X > p1.X)
                        return -1;
                    else
                        return 0;
                }
                double xPart = (p2.Y - p1.Y) * (p.X - p1.X) / (p2.X - p1.X) + p1.Y;
                if (p.Y < xPart)
                    return -1;
                if (p.Y > xPart)
                    return 1;
                else
                    return 0;
            }
            
            private Point ClosestToCenter(PointCollection points)
            {
                Point center = new Point(0, 0);
                Point closest = new Point(0, 0);
                double distanceToClosest = double.MaxValue;
                for (int i = 0; i < points.Count; i++)
                {
                    double currentDistance = DistanceBetweenPoints(center, points[i]);
                    if (currentDistance < distanceToClosest)
                    {
                        distanceToClosest = currentDistance;
                        closest = points[i];
                    }
                }
                return closest;
            }

            private Point FarestFromCenter(PointCollection points)
            {
                Point center = new Point(0, 0);
                Point farest = new Point(double.MaxValue, double.MaxValue);
                double distanceToFarest = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    double currentDistance = DistanceBetweenPoints(center, points[i]);
                    if (currentDistance > distanceToFarest)
                    {
                        distanceToFarest = currentDistance;
                        farest = points[i];
                    }
                }
                return farest;
            }
        }

        class RectanglularTriangleDrawingState : TriangleDrawingState
        {
            EditorAction _startDrawing3PointBase;
            EditorAction _continueDrawing3Base;
            public RectanglularTriangleDrawingState(IChangeDrawingState subject) : base(subject)
            {
                _startDrawing3PointBase = _startActions[2];
                _startActions[2] = (mouseDownPosition) =>
                {
                    _startDrawing3PointBase(RectangleTriangle3Point(_trianglePoints, mouseDownPosition));
                };

                _continueDrawing3Base = _continueActions[3];
                _continueActions[3] = (mouseDownPosition) =>
                {
                    _continueDrawing3Base(RectangleTriangle3Point(_trianglePoints, mouseDownPosition));
                };
            }

            Point RectangleTriangle3Point(PointCollection points, Point mouseDownPosition)
            {
                Point O = MiddleOfSegment(points[0], points[1]);
                double R = DistanceBetweenPoints(O, points[1]);
                double fromPointerToO = DistanceBetweenPoints(O, mouseDownPosition);
                double k = R / fromPointerToO;
                return new Point(O.X + k * (mouseDownPosition.X - O.X),
                    O.Y + k * (mouseDownPosition.Y - O.Y));
            }
        }

        class RegularTriangleDrawingState : TriangleDrawingState
        {
            EditorAction _startDrawing3PointBase;
            EditorAction _continueDrawing3PointBase;
            public RegularTriangleDrawingState(IChangeDrawingState subject) : base(subject)
            {
                _startDrawing3PointBase = _startActions[2];
                _startActions[2] = (mouseDownPosition) =>
                {
                    _startDrawing3PointBase(RegularTriangle3Point(_trianglePoints, mouseDownPosition));
                };

                _continueDrawing3PointBase = _continueActions[3];
                _continueActions[3] = (mouseDownPosition) =>
                {
                    _continueDrawing3PointBase(RegularTriangle3Point(_trianglePoints, mouseDownPosition));
                };
            }

            Point RegularTriangle3Point(PointCollection points, Point mouseDownPosition)
            {
                Point p1, p2;
                if (points[0].X > points[1].X)
                {
                    p1 = points[0];
                    p2 = points[1];
                }
                else
                {
                    p2 = points[0];
                    p1 = points[1];
                }
                Point O = MiddleOfSegment(p1, p2);
                Vector OP2 = p2 - O;
                double H = DistanceBetweenPoints(p1, p2) * Math.Sqrt(3) / 2;
                double sinBeta = OP2.Y / OP2.Length;
                double beta = Math.Asin(sinBeta);
                double degr90 = Math.PI / 2;
                double alpha = degr90 - beta;
                double deltaX = H * Math.Cos(alpha);
                double deltaY = H * Math.Sin(alpha);


                Point thirdPoint = new Point(O.X - deltaX, O.Y - deltaY);
                if (PositionOfPointYToLineBySegment(p1, p2, mouseDownPosition) > 0)
                {
                    thirdPoint.X = O.X + deltaX;
                    thirdPoint.Y = O.Y + deltaY;
                }
                return thirdPoint;
            }
        }

        /**
         * Editing state classes below
         **/

        Ellipse IChangeDrawingState.SetServicePoint(Point center, Canvas imagePlot = null)
        {
            UIElement image;
            if (imagePlot == null)
                image = (this as IChangeDrawingState).CurrentImage.Child;
            else
                image = imagePlot.Parent as Border;
            try
            {
                double offsetLeft = Canvas.GetLeft(image);
                double offsetTop = Canvas.GetTop(image);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Given image is invalid");
            }
            double width, height;
            width = height = 6;
            var servicePoint = new Ellipse()
            {
                Width = width,
                Height = height,
                Stroke = Brushes.SteelBlue,
                StrokeThickness = 1,
                Fill = Brushes.White
            };
            Canvas.SetLeft(servicePoint, center.X - servicePoint.Width / 2);
            Canvas.SetTop(servicePoint, center.Y - servicePoint.Height / 2);
            _canvas.Children.Add(servicePoint);
            return servicePoint;
        }

        void IChangeDrawingState.MoveServicePoint(Ellipse servicePoint, Point coordinates, Canvas imagePlot = null)
        {
            UIElement image;
            if (imagePlot == null)
                image = (this as IChangeDrawingState).CurrentImage.Child;
            else
                image = imagePlot.Parent as Border;
            try
            {
                double offsetLeft = Canvas.GetLeft(image);
                double offsetTop = Canvas.GetTop(image);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Given image is invalid");
            }
            Canvas.SetLeft(servicePoint, coordinates.X - servicePoint.Width / 2);
            Canvas.SetTop(servicePoint, coordinates.Y - servicePoint.Height / 2);
        }

        void IChangeDrawingState.RemoveServicePoint(Ellipse servicePoint)
        {
            _canvas.Children.Remove(servicePoint);
        }

        class RotateFigureState : EditorState
        {
            MouseEventArgs _cursor;
            KeyValuePair<Point, Ellipse> _centerPoint;
            double _basicMouseAngle;
            double _rotatedAngle;
            RotateTransform _rotateTransform;
            int _counter;
            public RotateFigureState(IChangeDrawingState subject) : base(subject)
            {
                _counter = 1;

                _startActions = new Dictionary<int, EditorAction>(2);
                _startActions[1] = (mouseDownPosition) =>
                {
                    _centerPoint = new KeyValuePair<Point, Ellipse>(
                        mouseDownPosition,
                        _subject.SetServicePoint(_cursor.GetPosition(_subject.Canvas))
                    );
                };
                _startActions[2] = (mouseDownPosition) =>
                {
                    _basicMouseAngle = GetNormalizedAngle(_centerPoint.Key, mouseDownPosition);
                    _rotatedAngle = _rotateTransform.Angle;
                };
                StartAction = (mouseDownPosition) =>
                {
                    _startActions[_counter](mouseDownPosition);
                };

                _continueActions = new Dictionary<int, EditorAction>(2);
                _continueActions[1] = (currentMousePosition) =>
                {
                    var pointOnCanvas = _cursor.GetPosition(_subject.Canvas);
                    _subject.MoveServicePoint(_centerPoint.Value, pointOnCanvas);
                    _centerPoint = new KeyValuePair<Point, Ellipse>(
                        currentMousePosition,
                        _centerPoint.Value
                    );
                };
                _continueActions[2] = (currentMousePosition) =>
                {
                    double rotateAngle = GetNormalizedAngle(_centerPoint.Key, currentMousePosition);
                    _rotateTransform.Angle = (_rotatedAngle + RadToDegrees(rotateAngle - _basicMouseAngle)) % 360;
                };
                ContinueAction = (currentMousePosition) =>
                {
                    _continueActions[_counter](currentMousePosition);
                };

                FinishAction = (endMousePosition) =>
                {
                    if (_counter == 1)
                    {
                        var transformCenter = _subject.GetRenderTransformCenter(_centerPoint.Key);
                        _rotateTransform = new RotateTransform(0, transformCenter.X, transformCenter.Y);
                        _subject.AddRenderTransform(_rotateTransform);
                        _counter++;
                    }
                };
                _subject.EditorModeChanged += (manager, newState) =>
                {
                    Reset();
                };
            }

            public override void Reset()
            {
                base.Reset();
                _counter = 1;
                _subject.RemoveServicePoint(_centerPoint.Value);
            }

            public override Point GetMousePostition(MouseEventArgs e)
            {
                _cursor = e;
                return base.GetMousePostition(e);
            }

            double GetNormalizedAngle(Point center, Point p)
            {
                double distance = DistanceBetweenPoints(p, center);
                return GetNormalizedAngle(Math.Asin((p.Y - center.Y) / distance), Math.Acos((p.X - center.X) / distance));
            }
            double GetNormalizedAngle(double angleToXAxisBySin, double angleToXAxisByCos)
            {
                if (double.IsNaN(angleToXAxisBySin) || double.IsNaN(angleToXAxisByCos))
                    return 0;
                double angleToXAxis;
                if (angleToXAxisBySin >= 0)
                {
                    angleToXAxis = angleToXAxisByCos;
                }
                else
                {
                    angleToXAxis = Math.PI;
                    if (angleToXAxisByCos < Math.PI / 2)
                    {
                        angleToXAxis += angleToXAxisBySin + Math.PI;
                    }
                    else
                    {
                        angleToXAxis -=  angleToXAxisBySin;
                    }
                }
                return angleToXAxis;
            }
            double RadToDegrees(double radians)
            {
                return radians * 180 / Math.PI;
            }
        }
        void IChangeDrawingState.AddRenderTransform(Transform transform)
        {
            AddRenderTransform(transform);
        }
        void AddRenderTransform(Transform transform)
        {
            TransformGroup transformGroup;
            if (_currentFigure.RenderTransform == null)
            {
                transformGroup = new TransformGroup();
                transformGroup.Children.Add(transform);
                _currentFigure.RenderTransform = transformGroup;
            }
            else if (_currentFigure.RenderTransform as TransformGroup == null)
            {
                var previousTransform = _currentFigure.RenderTransform;
                transformGroup = new TransformGroup();
                transformGroup.Children.Add(transform);
                _currentFigure.RenderTransform = transformGroup;
            }
            else
            {
                (_currentFigure.RenderTransform as TransformGroup).Children.Add(transform);
            }
        }

        class MoveFigureState : EditorState
        {
            Point _previousMousePosition;
            public MoveFigureState(IChangeDrawingState subject) : base(subject)
            {
                StartAction = (mouseDownPosition) =>
                {
                    if (_subject.CurrentFigure == null)
                        return;
                    _previousMousePosition = mouseDownPosition;
                };

                ContinueAction = (currentMousePosition) =>
                {
                    if (_subject.CurrentFigure == null)
                        return;
                    Point delta = new Point(
                        currentMousePosition.X - _previousMousePosition.X,
                        currentMousePosition.Y - _previousMousePosition.Y
                    );

                    // We won't diffenciate shapes and try doing so:
                    Canvas.SetLeft(_subject.CurrentFigure, Canvas.GetLeft(_subject.CurrentFigure) + delta.X);
                    Canvas.SetTop(_subject.CurrentFigure, Canvas.GetTop(_subject.CurrentFigure) + delta.Y);

                    _previousMousePosition = currentMousePosition;
                };
            }
        }
        class MoveImageState : EditorState
        {
            Point _previousMousePosition;
            public MoveImageState(IChangeDrawingState subject) : base(subject)
            {
                StartAction = (mouseDownPosition) =>
                {
                    if (_subject.CurrentImage == null)
                        return;
                    _previousMousePosition = mouseDownPosition;
                };

                ContinueAction = (currentMousePosition) =>
                {
                    if (_subject.CurrentImage == null)
                        return;
                    Point delta = new Point(
                        currentMousePosition.X - _previousMousePosition.X,
                        currentMousePosition.Y - _previousMousePosition.Y
                    );

                    Canvas.SetLeft(subject.CurrentImage, Canvas.GetLeft(subject.CurrentImage) + delta.X);
                    Canvas.SetTop(subject.CurrentImage, Canvas.GetTop(subject.CurrentImage) + delta.Y);

                    _previousMousePosition = currentMousePosition;
                };
            }
            public override Point GetMousePostition(MouseEventArgs e)
            {
                return e.GetPosition(_subject.Canvas);
            }
        }

        class ScaleFigureState : EditorState
        {
            MouseEventArgs _cursor;
            KeyValuePair<Point, Ellipse> _centerPoint;
            int _counter;
            Point _previousMousePosition;
            ScaleTransform _scaleTransform;
            public ScaleFigureState(IChangeDrawingState subject) : base(subject)
            {
                _counter = 1;

                _startActions = new Dictionary<int, EditorAction>(2);
                _startActions[1] = (mouseDownPosition) =>
                {
                    _centerPoint = new KeyValuePair<Point, Ellipse>(
                        mouseDownPosition,
                        _subject.SetServicePoint(_cursor.GetPosition(_subject.Canvas))
                    );
                };
                _startActions[2] = (mouseDownPoition) =>
                {
                    _previousMousePosition = mouseDownPoition;
                };
                StartAction = (mouseDownPosition) =>
                {
                    if (_subject.CurrentFigure == null)
                        return;
                    _startActions[_counter](mouseDownPosition);
                };

                _continueActions = new Dictionary<int, EditorAction>(2);
                _continueActions[1] = (currentMousePosition) =>
                {
                    var pointOnCanvas = _cursor.GetPosition(_subject.Canvas);
                    _subject.MoveServicePoint(_centerPoint.Value, pointOnCanvas);
                    _centerPoint = new KeyValuePair<Point, Ellipse>(
                        currentMousePosition,
                        _centerPoint.Value
                    );
                };
                _continueActions[2] = (currentMousePosition) =>
                {
                    double k = DistanceBetweenPoints(_centerPoint.Key, currentMousePosition) /
                        DistanceBetweenPoints(_centerPoint.Key, _previousMousePosition);
                    _scaleTransform.ScaleX = _scaleTransform.ScaleY *= k;
                    _previousMousePosition = currentMousePosition;
                };
                ContinueAction = (currentMousePosition) =>
                {
                    if (_subject.CurrentFigure == null)
                        return;
                    _continueActions[_counter](currentMousePosition);
                };

                FinishAction = (mouseUpPosition) =>
                {
                    if (_subject.CurrentFigure == null)
                        return;
                    if (_counter == 1)
                    {
                        var transformCenter = _subject.GetRenderTransformCenter(_centerPoint.Key);
                        _scaleTransform = new ScaleTransform(1, 1, transformCenter.X, transformCenter.Y);
                        _subject.AddRenderTransform(_scaleTransform);
                        _counter++;
                    }
                };
            }
            public override void Reset()
            {
                base.Reset();
                _subject.RemoveServicePoint(_centerPoint.Value);
                _counter = 1;
            }

            public override Point GetMousePostition(MouseEventArgs e)
            {
                _cursor = e;

                return base.GetMousePostition(e);
            }
        }

        class MergeImagesState : EditorState
        {
            Border _secondImage;
            public MergeImagesState(IChangeDrawingState subject) : base(subject)
            {
                _captureMouse = false;
                StartAction = (mouseDownPosition) =>
                {
                    if (_subject.CurrentImage == null)
                        return;
                    // Maybe not adding but replacing is needed
                };

                FinishAction = (mouseUpPosition) =>
                {
                    if (_subject.CurrentImage == null)
                        return;
                    SetSecondImageByMousePosition(mouseUpPosition);
                    if (_secondImage != null && _secondImage != _subject.CurrentImage)
                    {
                        MergeSecondImageIntoFirst(subject.CurrentImage, _secondImage);
                        subject.Canvas.Children.Remove(_secondImage);
                    }
                };
            }

            void SetSecondImageByMousePosition(Point mousePosition)
            {
                var images = _subject.Canvas.Children;
                for (int i = 0; i < images.Count; i++)
                    if (images[i].IsMouseOver)
                        _secondImage = images[i] as Border;
            }

            // merge second image into first (current) image
            void MergeSecondImageIntoFirst(Border firstImage, Border secondImage)
            {
                double maxWidth = Math.Max(firstImage.Width, secondImage.Width);
                double maxHeight = Math.Max(firstImage.Height, secondImage.Height);
                var firstCanvas = firstImage.Child as Canvas;
                var secondCanvas = secondImage.Child as Canvas;
                firstImage.Width = maxWidth;
                firstCanvas.Width = maxWidth - firstImage.BorderThickness.Right - firstImage.BorderThickness.Left;
                firstImage.Height = maxHeight;
                firstCanvas.Height = maxHeight - firstImage.BorderThickness.Top - firstImage.BorderThickness.Bottom;

                var firstImageFiguresCollection = firstCanvas.Children;
                var secondImageChildren = secondCanvas.Children;
                for (int i = 0; i < secondImageChildren.Count;)
                {
                    // Assume that CanvasLeft and top are not changed
                    var figure = (Shape)secondImageChildren[i];
                    secondImageChildren.RemoveAt(i);
                    firstImageFiguresCollection.Add(figure);
                }
            }

            public override Point GetMousePostition(MouseEventArgs e)
            {
                return e.GetPosition(_subject.Canvas);
            }
        }
    }
}
