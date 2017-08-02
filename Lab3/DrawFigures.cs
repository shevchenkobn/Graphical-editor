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
            public EditorAction StartAction;
            public EditorAction ContinueAction;
            public EditorAction FinishAction;
            public EditorState(IChangeDrawingState subject)
            {
                _subject = subject;
                StartAction = new EditorAction((MouseDownPosition) => { });
                ContinueAction = new EditorAction((currentMousePosition) => { });
                FinishAction = new EditorAction((mouseUpPostion) => { });
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

        // Stub state class
        class UndefinedState : EditorState
        {
            public UndefinedState(DrawingManager subject) : base(subject)
            {
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
                    _image = new Border() { Height = 0, Width = 0 };

                    Canvas.SetLeft(_image, mouseDownPosition.X);
                    Canvas.SetTop(_image, mouseDownPosition.Y);
                    subject.Canvas.Children.Add(_image);
                    subject.CurrentImage = _image;
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
                    if (_image.Width == 0 || _image.Height == 0)
                    {
                        subject.Canvas.Children.Remove(_image);
                    }
                    else
                    {
                        //hm... maybe some other manipulations needed before these
                        //image.BorderThickness = preferences.ImageBorderThickness;
                        //image.BorderBrush = preferences.ImageBorder;
                        //image.Focusable = true;
                        _image.Child = new Canvas()
                        {
                            //Background = preferences.ImageBackground,
                            Width = _image.Width - _image.BorderThickness.Left - _image.BorderThickness.Right,
                            Height = _image.Height - _image.BorderThickness.Top - _image.BorderThickness.Bottom,
                            ClipToBounds = true
                        };
                        // We can focus this image or not
                        subject.CurrentImage = _image;
                        //image.Focus();
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
                    (subject.CurrentImage.Child as Canvas).Children.Add(polyline);
                    subject.CurrentFigure = polyline;
                    _trianglePoints = new PointCollection();
                    _trianglePoints.Add(mouseDownPosition);
                    (subject.CurrentFigure as Polyline).Points = _trianglePoints;
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
                    var image = subject.CurrentImage.Child as Canvas;
                    Canvas.SetTop(triangle, Canvas.GetTop(subject.CurrentFigure));
                    Canvas.SetLeft(triangle, Canvas.GetLeft(subject.CurrentFigure));
                    image.Children.Remove(subject.CurrentFigure);
                    image.Children.Add(triangle);
                    subject.CurrentFigure = triangle;
                    // We can focus this figure or not
                    //subject.CurrentFigure.Focus();
                };
                StartAction = (mouseDownPosition) =>
                {
                    _startActions[_trianglePoints.Count](mouseDownPosition);
                };

                _continueActions = new Dictionary<int, EditorAction>(3);

                _continueActions[1] = (currentCursorPosition) =>
                {
                    (subject.CurrentFigure as Polyline).Points.Add(currentCursorPosition);
                };
                _continueActions[2] = (currentCursorPosition) =>
                {
                    (subject.CurrentFigure as Polyline).Points[1] = currentCursorPosition;
                };
                _continueActions[3] = (currentCursorPosition) =>
                {
                    (subject.CurrentFigure as Polygon).Points[2] = currentCursorPosition;
                };
                ContinueAction = (currentCursorPosition) =>
                {
                    if (subject.IsOperationInProcess && subject.Canvas.CaptureMouse())
                        _continueActions[_trianglePoints.Count](currentCursorPosition);
                };

                FinishAction = (mouseUpPosition) =>
                {
                    if (_trianglePoints.Count == 3)
                    {
                         if (PositionOfPointYToLineBySegment(_trianglePoints[0], _trianglePoints[1], _trianglePoints[2]) == 0)
                         {
                             (subject.CurrentImage.Child as Canvas).Children.Remove(subject.CurrentFigure);
                             subject.CurrentFigure = null;
                         }
                         Reset();
                    }
                    subject.Canvas.ReleaseMouseCapture();
                };
            }

            public override void Reset()
            {
                base.Reset();
                _trianglePoints = new PointCollection();
            }
            protected Point MiddleOfSegment(Point a, Point b)
            {
                return new Point(a.X + (b.X - a.X) / 2, a.Y + (b.Y - a.Y) / 2);
            }
            protected int PositionOfPointYToLineBySegment(Point p1, Point p2, Point p)
            {
                double xPart = (p2.Y - p1.Y) * (p.X - p1.X) / (p2.X - p1.X) + p1.Y;
                if (p.Y > xPart)
                    return 1;
                if (p.Y < xPart)
                    return -1;
                else
                    return 0;
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
            Canvas.SetLeft(servicePoint, center.Y - servicePoint.Height / 2);
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
            Canvas.SetLeft(servicePoint, coordinates.Y - servicePoint.Height / 2);
        }

        void IChangeDrawingState.RemoveServicePoint(Ellipse servicePoint)
        {
            _canvas.Children.Remove(servicePoint);
        }

        class RotateFigureState : EditorState
        {
            KeyValuePair<Point, Ellipse> _centerPoint;
            RotateTransform _rotateTransform;
            int _counter;
            public RotateFigureState(IChangeDrawingState subject) : base(subject)
            {
                _counter = 1;

                StartAction = (mouseDownPosition) =>
                {
                    if (_counter == 1)
                    {
                        _centerPoint = new KeyValuePair<Point, Ellipse>(
                            mouseDownPosition,
                            subject.SetServicePoint(mouseDownPosition)
                        );
                    }
                    
                };

                _continueActions = new Dictionary<int, EditorAction>(2);
                _continueActions[1] = (currentMousePosition) =>
                {
                    subject.MoveServicePoint(_centerPoint.Value, currentMousePosition);
                    _centerPoint = new KeyValuePair<Point, Ellipse>(
                        currentMousePosition,
                        _centerPoint.Value
                    );
                };
                _continueActions[2] = (currentMousePosition) =>
                {
                    double rotateAngle = GetNormalizedAngle(_centerPoint.Key, currentMousePosition);
                    _rotateTransform.Angle += rotateAngle;
                };
                ContinueAction = (currentMousePosition) =>
                {
                    _continueActions[_counter](currentMousePosition);
                };

                FinishAction = (endMousePosition) =>
                {
                    if (_counter == 1)
                    {
                        _rotateTransform = (RotateTransform)subject.AddRenderTransform(new RotateTransform(0, _centerPoint.Key.X, _centerPoint.Key.Y));
                    }
                };
                subject.EditorModeChanged += (manager, newState) =>
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

            double GetNormalizedAngle(Point p, Point center)
            {
                double distance = DistanceBetweenPoints(p, center);
                return GetNormalizedAngle(Math.Asin(p.Y / distance), Math.Acos(p.X / distance));
            }
            double GetNormalizedAngle(double angleToXAxisBySin, double angleToXAxisByCos)
            {
                double angleToXAxis;
                if (angleToXAxisBySin > 0)
                {
                    angleToXAxis = angleToXAxisByCos;
                }
                else
                {
                    if (angleToXAxisByCos < Math.PI / 2 && angleToXAxisByCos <= Math.PI)
                    {
                        angleToXAxis = Math.PI - angleToXAxisBySin;
                    }
                    else
                    {
                        angleToXAxis = angleToXAxisBySin;
                    }
                }
                return angleToXAxis;
            }
        }
        Transform IChangeDrawingState.AddRenderTransform(Transform transform)
        {
            return AddRenderTransform(transform);
        }
        Transform AddRenderTransform(Transform transform)
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
                if (previousTransform.GetType() == transform.GetType())
                {
                    transformGroup.Children.Add(previousTransform);
                }
                else
                {
                    transform = previousTransform;
                }
                transformGroup.Children.Add(transform);
                _currentFigure.RenderTransform = transformGroup;
            }
            else
            {
                transformGroup = _currentFigure.RenderTransform as TransformGroup;
                bool found = false;
                var transformType = transform.GetType();
                foreach (var transfrormFromGroup in transformGroup.Children)
                {
                    if (transfrormFromGroup.GetType() == transform.GetType())
                    {
                        transform = transfrormFromGroup;
                        found = true;
                        break;
                    }
                }
                if (found)
                    transformGroup.Children.Add(transform);
            }
            return transform;
        }

        class MoveFigureState : EditorState
        {
            Point _previousMousePosition;
            public MoveFigureState(IChangeDrawingState subject) : base(subject)
            {
                StartAction = (mouseDownPosition) =>
                {
                    _previousMousePosition = mouseDownPosition;
                };

                ContinueAction = (currentMousePosition) =>
                {
                    Point delta = new Point(
                        currentMousePosition.X - _previousMousePosition.X,
                        currentMousePosition.Y - _previousMousePosition.Y
                    );

                    // We won't diffenciate shapes and try doing so:
                    Canvas.SetLeft(subject.CurrentFigure, Canvas.GetLeft(subject.CurrentFigure) + delta.X);
                    Canvas.SetTop(subject.CurrentFigure, Canvas.GetTop(subject.CurrentFigure) + delta.Y);

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
                    _previousMousePosition = mouseDownPosition;
                };

                ContinueAction = (currentMousePosition) =>
                {
                    Point delta = new Point(
                        currentMousePosition.X - _previousMousePosition.X,
                        currentMousePosition.Y - _previousMousePosition.Y
                    );

                    Canvas.SetLeft(subject.Canvas, Canvas.GetLeft(subject.CurrentImage) + delta.X);
                    Canvas.SetTop(subject.Canvas, Canvas.GetTop(subject.CurrentImage) + delta.Y);

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
                        subject.SetServicePoint(mouseDownPosition)
                    );
                };
                _startActions[2] = (mouseDownPoition) =>
                {
                    _previousMousePosition = mouseDownPoition;
                };
                StartAction = (mouseDownPosition) =>
                {
                    _startActions[_counter](mouseDownPosition);
                };

                _continueActions = new Dictionary<int, EditorAction>(2);
                _continueActions[1] = (currentMousePosition) =>
                {
                    subject.MoveServicePoint(_centerPoint.Value, currentMousePosition);
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
                };

                FinishAction = (mouseUpPosition) =>
                {
                    if (_counter == 1)
                    {
                        _scaleTransform = (ScaleTransform)subject.AddRenderTransform(new ScaleTransform(1, 1, _centerPoint.Key.X, _centerPoint.Key.Y));
                        _counter++;
                    }
                };
            }
            public override void Reset()
            {
                base.Reset();
                _counter = 1;
            }
        }

        class MergeImagesState : EditorState
        {
            MouseButtonEventHandler _captureSecondImage = (o, e) =>
            {

            };
            Border _secondImage;
            public MergeImagesState(IChangeDrawingState subject) : base(subject)
            {
                StartAction = (mouseDownPosition) =>
                {
                    // Maybe not adding but replacing is needed
                    subject.CurrentImage.MouseUp += _captureSecondImage;
                };

                FinishAction = (mouseDownPosition) =>
                {
                    mergeSecondImageIntoFirst(subject.CurrentImage, _secondImage);
                    subject.Canvas.Children.Remove(_secondImage);
                    subject.CurrentImage.MouseUp -= _captureSecondImage;
                };
            }

            // merge second image into first (current) image
            void mergeSecondImageIntoFirst(Border firstImage, Border secondImage)
            {
                firstImage.Width = Math.Max(firstImage.Width, secondImage.Width);
                firstImage.Height = Math.Max(firstImage.Height, secondImage.Height);

                var firstImageFiguresCollection = (firstImage.Child as Canvas).Children;
                foreach (var figure in (secondImage.Child as Canvas).Children)
                {
                    // Assume that CanvasLeft and top are not changed
                    firstImageFiguresCollection.Add((Shape)figure);
                }
            }
        }
    }
}
