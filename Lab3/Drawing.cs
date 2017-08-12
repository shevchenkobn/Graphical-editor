using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Lab3
{
    public enum EditorMode
    {
        SwitchElements,
        DrawImage,
        DrawTriangle, DrawRectangularTriangle, DrawRegularTriangle,
        RotateFigure, MoveFigure, MoveImage, ScaleFigure, MergeImages
    }

    public delegate void EditorModeChangedEventHandler(IDrawingManager manager, EditorMode newMode);
    public delegate void ImageCurrentStatusChangedEventHandler(IDrawingManager manager, Border image);
    public delegate void FigureCurrentStatusChangedEventHandler(IDrawingManager manager, Shape figure);
    public interface IDrawingManager
    {
        void SetCanvas(Canvas canvas);
        EditorPreferences Preferences { get; }
        EditorMode CurrentMode { get; set; }
        event EditorModeChangedEventHandler EditorModeChanged;
        event ImageCurrentStatusChangedEventHandler ImageCurrentStatusLost;
        event ImageCurrentStatusChangedEventHandler ImageCurrentStatusGot;
        event FigureCurrentStatusChangedEventHandler FigureCurrentStatusLost;
        event FigureCurrentStatusChangedEventHandler FigureCurrentStatusGot;
        void StartAction(MouseEventArgs e);
        void ContinueAction(MouseEventArgs e);
        void FinishAction(MouseEventArgs e);
        void StartAction(Point mouseDownPosition);
        void ContinueAction(Point currentMousePosition);
        void FinishAction(Point mouseUpPosition);
        void DeleteCurrentImage();
        void DeleteCurrentFigure();
        void ClearCanvas();
        bool IsOperationInProcess { get; }
    }
    public class EditorPreferences
    {
        public Brush ImageBorder { get; set; } = Brushes.SteelBlue;
        public Thickness ImageBorderThickness { get; set; } = new Thickness(1);
        public Brush ImageCanvasBackground { get; set; } = Brushes.White;

        public Thickness CurrentImageBorderThickness { get; set; } = new Thickness(2);
        public Brush CurrentImageBorderBrush { get; set; } = Brushes.SeaGreen;

        public double FigureStrokeThickness { get; set; } = 1;
        public Brush FigureStroke { get; set; } = Brushes.SteelBlue;
        public Brush FigureFilling { get; set; } = Brushes.Transparent;//LightSeaGreen;
        public Brush FigureBackground { get; set; } = Brushes.Transparent;

        public Brush CurrentFigureStroke { get; set; } = Brushes.SteelBlue;
        public double CurrentFigureStrokeThickness { get; set; } = 2;
    }
    interface IChangeDrawingState
    {
        void SwitchState(EditorMode newState);
        event EditorModeChangedEventHandler EditorModeChanged;
        Shape CurrentFigure { get; set; }
        Border CurrentImage { get; set; }
        Canvas Canvas { get; }
        //EditorPreferences Preferences { get; }
        // Setting of service point method
        // Service point is used to mark some aspects of editing, e.g. center of rotation
        Ellipse SetServicePoint(Point center, Canvas imagePlot = null);
        // Method to remove service point
        void RemoveServicePoint(Ellipse servicePoint);
        void MoveServicePoint(Ellipse servicePoint, Point center, Canvas imagePlot = null);
        void AddRenderTransform(Transform transfrom);
        bool IsOperationInProcess { get; set; }
        //void ChangeCurrentImage(Border image);
        //void ChangeCurrentFigure(Shape figure);
    }
    partial class DrawingManager : IDrawingManager, IChangeDrawingState
    {
        Canvas _canvas;
        bool _isActionStarted;
        Canvas IChangeDrawingState.Canvas { get => _canvas; }
        EditorState _state;
        EditorPreferences _preferences;
        public EditorPreferences Preferences { get { return _preferences; } }
        Dictionary<EditorMode, EditorState> _states;

        static DrawingManager _instance;

        public static DrawingManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DrawingManager();
                return _instance;
            }
        }
        DrawingManager()
        {
            _preferences = new EditorPreferences();
            _states = new Dictionary<EditorMode, EditorState>();
            _states[EditorMode.SwitchElements] = new SwitchElementState(this);

            // Initialize all state classes here
            _states[EditorMode.DrawImage] = new ImageDrawingState(this);

            _states[EditorMode.DrawTriangle] = new TriangleDrawingState(this);
            _states[EditorMode.DrawRectangularTriangle] = new RectanglularTriangleDrawingState(this);
            _states[EditorMode.DrawRegularTriangle] = new RegularTriangleDrawingState(this);

            _states[EditorMode.RotateFigure] = new RotateFigureState(this);
            _states[EditorMode.MoveFigure] = new MoveFigureState(this);
            _states[EditorMode.MoveImage] = new MoveImageState(this);
            _states[EditorMode.ScaleFigure] = new ScaleFigureState(this);
            _states[EditorMode.MergeImages] = new MergeImagesState(this);
            //////

            _state = _states[EditorMode.SwitchElements];
            CurrentMode = EditorMode.SwitchElements;
        }

        public event EditorModeChangedEventHandler EditorModeChanged;
        private EditorMode _currentMode;
        public EditorMode CurrentMode {
            get => _currentMode;

            set
            {
                if (_canvas != null)
                {
                    _currentMode = value;
                    EditorModeChanged?.Invoke(this, value);
                    SwitchState(value);
                }
            }
        }

        Shape IChangeDrawingState.CurrentFigure {
            get => _currentFigure;
            set
            {
                if (value != null && (_currentImage?.Child as Canvas).Children.Contains(value) || value == null)
                {
                    if (_currentFigure != null)
                    {
                        FigureCurrentStatusLost?.Invoke(this, _currentFigure);
                    }
                    _currentFigure = value;
                    if (_currentFigure != null)
                    {
                        FigureCurrentStatusGot?.Invoke(this, _currentFigure);
                    }
                }
            }
        }
        Shape _currentFigure;
        public event FigureCurrentStatusChangedEventHandler FigureCurrentStatusLost;
        public event FigureCurrentStatusChangedEventHandler FigureCurrentStatusGot;
        Border IChangeDrawingState.CurrentImage
        {
            get => _currentImage;
            set
            {
                if (value != null && _canvas.Children.Contains(value) || value == null)
                {
                    if (_currentImage != null)
                    {
                        ImageCurrentStatusLost?.Invoke(this, _currentImage);
                    }
                    _currentImage = value;
                    if (_currentImage != null)
                        ImageCurrentStatusGot?.Invoke(this, _currentImage);
                }
            }
        }
        Border _currentImage;
        public event ImageCurrentStatusChangedEventHandler ImageCurrentStatusLost;
        public event ImageCurrentStatusChangedEventHandler ImageCurrentStatusGot;
        
        public void StartAction(MouseEventArgs e)
        {
            StartAction(_state.GetMousePostition(e));
        }
        public void StartAction(Point mouseDownPosition)
        {
            _isActionStarted = true;
            _state.StartAction(mouseDownPosition);
            if (_state.CaptureMouse)
                _canvas.CaptureMouse();
        }

        public void ContinueAction(MouseEventArgs e)
        {
            ContinueAction(_state.GetMousePostition(e));
        }
        public void ContinueAction(Point currentMousePosition)
        {
            if (_isActionStarted)
                _state.ContinueAction(currentMousePosition);
        }

        public void FinishAction(MouseEventArgs e)
        {
            FinishAction(_state.GetMousePostition(e));
        }
        public void FinishAction(Point mouseUpPosition)
        {
            if (_isActionStarted)
                _state.FinishAction(mouseUpPosition);
            _isActionStarted = false;
            if (_state.CaptureMouse)
                _canvas.ReleaseMouseCapture();
        }

        bool _isOperationInProcess;
        public bool IsOperationInProcess
        {
            get => _isOperationInProcess;
            set
            {
                _isOperationInProcess = value;
                if (_currentImage != null)
                    ImageCurrentStatusGot?.Invoke(this, _currentImage);
                if (_currentFigure != null)
                    FigureCurrentStatusGot?.Invoke(this, _currentFigure);
            }
        }

        public void SetCanvas(Canvas canvas)
        {
            if (_canvas == null)
            {
                _canvas = canvas;
                _canvas.ClipToBounds = true;
                _canvas.IsHitTestVisible = true;
                if (_canvas.Background == null)
                    _canvas.Background = Brushes.Transparent;
            }
        }

        void SwitchState(EditorMode newState)
        {
            _state.Reset();
            _state = _states[newState];
        }
        void IChangeDrawingState.SwitchState(EditorMode newState)
        {
            SwitchState(newState);
        }
        void IDrawingManager.DeleteCurrentImage()
        {
            if (_currentImage != null)
            {
                var self = this as IChangeDrawingState;
                if ((_currentImage.Child as Canvas).Children.Contains(_currentFigure))
                    self.CurrentFigure = null;
                _canvas.Children.Remove(_currentImage);
                self.CurrentImage = null;
                //CurrentMode = default(EditorMode);
            }
        }

        void IDrawingManager.DeleteCurrentFigure()
        {
            if (_currentFigure != null)
            {
                (_currentFigure.Parent as Canvas).Children.Remove(_currentFigure);
                (this as IChangeDrawingState).CurrentFigure = null;
            }
        }

        void IDrawingManager.ClearCanvas()
        {
            var self = this as IChangeDrawingState;
            self.CurrentFigure = null;
            self.CurrentImage = null;
            if (_canvas != null)
                _canvas.Children.Clear();
        }
    }
}
