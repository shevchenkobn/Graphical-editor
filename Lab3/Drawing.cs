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
    public enum EditorMode
    {
        Undefined,
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
        bool IsOperationInProcess();
    }
    public class EditorPreferences
    {
        public Brush ImageBorder { get; set; } = Brushes.SteelBlue;
        public Thickness ImageBorderThickness { get; set; } = new Thickness(1);
        public double FigureStrokeThickness { get; set; } = 1;
        public Thickness FocusedImageBorderThickness { get; set; } = new Thickness(2);
        public double FigureFocusBorder { get; set; } = 2;
        public Brush FocusedImageBorder { get; set; } = Brushes.SeaGreen;
        public Brush FigureStroke { get; set; } = Brushes.SteelBlue;
        public Brush FigureFilling { get; set; } = Brushes.Transparent;//LightSeaGreen;
        public Brush FigureBackground { get; set; } = Brushes.Transparent;
        public Brush ImageBackground { get; set; } = Brushes.White;
        //public RoutedEventHandler ImageBorder_GotFocus { get; set; } = (o, e) =>
        //{

        //};
        //public RoutedEventHandler ImageBorder_LostFocus { get; set; }
        //public MouseButtonEventHandler ImageBorder_PreviewMouseLeftButtonDown { get; set; } = (o, e) =>
        //{
        //    (o as Border).Focus();
        //};
        //public RoutedEventHandler Figure_GotFocus { get; set; }
        //public RoutedEventHandler Figure_LostFocus { get; set; }
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
        Transform AddRenderTransform(Transform transfrom);
        bool IsOperationInProcess { get; set; }
        //void ChangeCurrentImage(Border image);
        //void ChangeCurrentFigure(Shape figure);
    }
    partial class DrawingManager : IDrawingManager, IChangeDrawingState
    {
        Canvas _canvas;
        bool _isActionStarted;
        Canvas IChangeDrawingState.Canvas { get; }
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
            _states[EditorMode.Undefined] = new UndefinedState(this);

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

            CurrentMode = EditorMode.Undefined;
        }

        public event EditorModeChangedEventHandler EditorModeChanged;
        public EditorMode CurrentMode {
            get => CurrentMode;

            set
            {
                if (_canvas != null)
                {
                    CurrentMode = value;
                    EditorModeChanged?.Invoke(this, value);
                    SwitchState(value);
                }
            }
        }

        Shape IChangeDrawingState.CurrentFigure {
            get => _currentFigure;
            set
            {
                if (value != null && (_currentImage?.Child as Canvas).Children.Contains(_currentFigure = value))
                {
                    if (_currentFigure != null)
                    {
                        FigureCurrentStatusLost?.Invoke(this, _currentFigure);
                    }
                    _currentFigure = value;
                    FigureCurrentStatusGot?.Invoke(this, _currentFigure);
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
                if (value != null && _canvas.Children.Contains(value))
                {
                    if (_currentImage != null)
                    {
                        ImageCurrentStatusLost?.Invoke(this, _currentImage);
                    }
                    _currentImage = value;
                    ImageCurrentStatusGot?.Invoke(this, _currentImage);
                }
            }
        }
        Border _currentImage;
        public event ImageCurrentStatusChangedEventHandler ImageCurrentStatusLost;
        public event ImageCurrentStatusChangedEventHandler ImageCurrentStatusGot;
        
        public void StartAction(MouseEventArgs e)
        {
            _state.StartAction(_state.GetMousePostition(e));
            _isOperationInProcess = true;
            _isActionStarted = true;
        }
        public void StartAction(Point mouseDownPosition)
        {
            _state.StartAction(mouseDownPosition);
            _isOperationInProcess = true;
            _isActionStarted = true;
        }

        public void ContinueAction(MouseEventArgs e)
        {
            if (_isActionStarted)
                _state.ContinueAction(_state.GetMousePostition(e));
        }
        public void ContinueAction(Point currentMousePosition)
        {
            if (_isActionStarted)
                _state.ContinueAction(currentMousePosition);
        }

        public void FinishAction(MouseEventArgs e)
        {
            if (_isActionStarted)
                _state.FinishAction(_state.GetMousePostition(e));
        }
        public void FinishAction(Point mouseUpPosition)
        {
            if (_isActionStarted)
                _state.FinishAction(mouseUpPosition);
        }

        bool _isOperationInProcess;
        bool IChangeDrawingState.IsOperationInProcess { get => _isOperationInProcess; set => _isOperationInProcess = value; }
        public bool IsOperationInProcess()
        {
            return _isOperationInProcess;
        }

        public void SetCanvas(Canvas canvas)
        {
            if (_canvas == null)
            {
                _canvas = canvas;
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
    }
}
