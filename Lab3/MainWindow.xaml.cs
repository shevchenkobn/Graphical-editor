using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lab3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string helpText = "Use toolbar on the left and menus on the top to draw some elements.";
        const string helpCapture = "Help";
        protected IDrawingManager _model;
        const string defaultStatusBarContent = "Use buttons on the left to draw";
        public MainWindow()
        {
            InitializeComponent();
            _model = DrawingManager.Instance;
            _model.SetCanvas(DrawingField);

            _model.ImageCurrentStatusLost += _model_ImageCurrentStatusLost;
            _model.ImageCurrentStatusLost += DeactivateAllButtons;
            _model.ImageCurrentStatusGot += _model_ImageCurrentStatusGot;
            _model.ImageCurrentStatusGot += ActivateAllButtons;
            _model.FigureCurrentStatusLost += _model_FigureCurrentStatusLost;
            _model.FigureCurrentStatusGot += _model_FigureCurrentStatusGot;
        }

        private void DeactivateAllButtons(IDrawingManager manager, Border image)
        {
            var buttons = DrawButtonsToolbar.Items;
            foreach (var button in buttons)
                if (button != NewImage)
                    (button as UIElement).IsEnabled = false;
        }

        private void ActivateAllButtons(IDrawingManager manager, Border image)
        {
            var buttons = DrawButtonsToolbar.Items;
            foreach (var button in buttons)
                (button as UIElement).IsEnabled = true;
        }

        private void _model_FigureCurrentStatusGot(IDrawingManager manager, Shape figure)
        {
            if (!manager.IsOperationInProcess)
            {
                figure.Stroke = manager.Preferences.FigureStroke;
                figure.StrokeThickness = manager.Preferences.FigureStrokeThickness;
            }
            else
                _model_FigureCurrentStatusLost(manager, figure);
        }

        private void _model_FigureCurrentStatusLost(IDrawingManager manager, Shape figure)
        {
            figure.Stroke = manager.Preferences.FigureStroke;
            figure.Fill = manager.Preferences.FigureFilling;
            figure.StrokeThickness = manager.Preferences.FigureStrokeThickness;
        }

        private void _model_ImageCurrentStatusGot(IDrawingManager manager, Border image)
        {
            if (!manager.IsOperationInProcess)
            {
                image.BorderBrush = manager.Preferences.FocusedImageBorderBrush;
                image.BorderThickness = manager.Preferences.FocusedImageBorderThickness;
            }
            else
                _model_ImageCurrentStatusLost(manager, image);
        }

        private void _model_ImageCurrentStatusLost(IDrawingManager manager, Border image)
        {
            image.BorderBrush = manager.Preferences.ImageBorder;
            image.BorderThickness = manager.Preferences.ImageBorderThickness;
            image.Background = manager.Preferences.ImageBackground;
        }

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show(this, helpText, helpCapture);
        }

        private void CanHelpExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        /// <summary>
        /// Will save the images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanLoadContentToCanvas(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void LoadContentToCanvas(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void SaveContentFromCanvas(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void CanSaveContentFromCanvas(object sender, CanExecuteRoutedEventArgs e)
        {

        }
        ///

        private void NewImage_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.DrawImage)
                _model.CurrentMode = EditorMode.DrawImage;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }

        private void Triangle_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.DrawTriangle)
                _model.CurrentMode = EditorMode.DrawTriangle;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }

        private void RectangularTriangle_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.DrawRectangularTriangle)
                _model.CurrentMode = EditorMode.DrawRectangularTriangle;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }

        private void RegualarTriangle_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.DrawRegularTriangle)
                _model.CurrentMode = EditorMode.DrawRegularTriangle;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }

        internal void DrawingField_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            _model.StartAction(e);
        }

        private void DrawingField_MouseMove(object sender, MouseEventArgs e)
        {
            _model.ContinueAction(e);
        }

        private void DrawingField_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _model.FinishAction(e);
        }

        private void MoveImage_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.MoveImage)
                _model.CurrentMode = EditorMode.MoveImage;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }
    }
}
