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
using Microsoft.Win32;
using System.IO;
using System.Windows.Markup;

namespace Lab3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string HelpText = "Use toolbar on the left and menus on the top to draw some elements.";
        const string HelpCapture = "Help";
        const string SaveImagesDelimeter = "\r\n";

        const string DefaultStatusBarContent = "Use buttons on the left to draw";
        Dictionary<EditorMode, string> _statusBarOptions;
        protected IDrawingManager _model;
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

            _statusBarOptions = new Dictionary<EditorMode, string>()
            {
                {EditorMode.SwitchElements, "Click on objects to focus them" },
                {EditorMode.DrawImage, "Draw new Image by mouse" },
                {EditorMode.DrawTriangle, "Draw new triangle by mouse" },
                {EditorMode.DrawRegularTriangle, "Draw new regular triagnle by mouse" },
                {EditorMode.DrawRectangularTriangle, "Draw new rectangular triangle by mouse" },
                {EditorMode.MoveFigure, "Move focused figure using your mouse" },
                {EditorMode.MoveImage, "Move focused Image using your mouse" },
                {EditorMode.RotateFigure, "Rotate focused figure around arbitrary center" },
                {EditorMode.ScaleFigure, "Scale focused image relatively to an center" },
                {EditorMode.MergeImages, "Merge some other image into focused one" }
            };
            _model.EditorModeChanged += _model_EditorModeChanged;
        }

        private void _model_EditorModeChanged(IDrawingManager manager, EditorMode newMode)
        {
            StatusBarText.Text = _statusBarOptions[newMode];
        }

        private void DeactivateAllButtons(IDrawingManager manager, Border image)
        {
            var buttons = DrawButtonsToolbar.Items;
            foreach (var button in buttons)
                if (button != NewImage || button != SwitchElements)
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
                figure.Stroke = manager.Preferences.CurrentFigureStroke;
                figure.StrokeThickness = manager.Preferences.CurrentFigureStrokeThickness;
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
                image.BorderBrush = manager.Preferences.CurrentImageBorderBrush;
                image.BorderThickness = manager.Preferences.CurrentImageBorderThickness;
                if (image.Child != null)
                    (image.Child as Canvas).Background = manager.Preferences.ImageCanvasBackground;
            }
            else
                _model_ImageCurrentStatusLost(manager, image);
        }

        private void _model_ImageCurrentStatusLost(IDrawingManager manager, Border image)
        {
            image.BorderBrush = manager.Preferences.ImageBorder;
            image.BorderThickness = manager.Preferences.ImageBorderThickness;
        }

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show(this, HelpText, HelpCapture);
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
            e.CanExecute = true;
        }

        private void LoadContentToCanvas(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Canvas file (.gcanv) | *.gcanv",
                InitialDirectory = Directory.GetCurrentDirectory(),
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                for (int i = 0; i < openFileDialog.FileNames.Length; i++)
                    LoadContentToCanvas(File.ReadAllText(openFileDialog.FileNames[i]));
            }   
        }

        private void SaveContentFromCanvas(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                AddExtension = true,
                FileName = "MyCanvas.gcanv",
                //Filter = "All Supported Extensions | *.gimage; *.gcanv | Image | *.gimage | Entire canvas | *.gcanv",
                Filter = "Canvas file (.gcanv) | *.gcanv",
                InitialDirectory = Directory.GetCurrentDirectory()
            };
            if (dialog.ShowDialog() == true)
                File.WriteAllText(dialog.FileName, XamlConvert());
        }

        private void CanSaveContentFromCanvas(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DrawingField.Children.Count > 0;
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

        private void SwitchElements_Click(object sender, RoutedEventArgs e)
        {
            _model.CurrentMode = EditorMode.SwitchElements;
        }

        private void RotateFigure_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.RotateFigure)
                _model.CurrentMode = EditorMode.RotateFigure;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }

        private void MoveFigure_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.MoveFigure)
                _model.CurrentMode = EditorMode.MoveFigure;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }

        private void ScaleFigure_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.ScaleFigure)
                _model.CurrentMode = EditorMode.ScaleFigure;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }

        private void MergeImages_Click(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentMode != EditorMode.MergeImages)
                _model.CurrentMode = EditorMode.MergeImages;
            else
                _model.CurrentMode = EditorMode.SwitchElements;
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            _model.DeleteCurrentImage();
        }

        private void DeleteFigure_Click(object sender, RoutedEventArgs e)
        {
            _model.DeleteCurrentFigure();
        }

        private string XamlConvert(Border image)
        {
            return XamlWriter.Save(image);
        }
        private string XamlConvert()
        {
            string xaml = "";
            foreach (var image in DrawingField.Children)
                xaml += XamlConvert(image as Border) + SaveImagesDelimeter;
            return xaml;
        }
        private Border[] XamlConvertCanvas(string xaml)
        {
            string[] encodedImages = xaml.Split(new string[] { SaveImagesDelimeter }, StringSplitOptions.RemoveEmptyEntries);
            var images = new Border[encodedImages.Length];
            for (int i = 0; i < encodedImages.Length; i++)
                images[i] = XamlConvertImage(encodedImages[i]);
            return images;
        }
        private Border XamlConvertImage(string xaml)
        {
            return XamlReader.Parse(xaml) as Border;
        }
        private void LoadContentToCanvas(string xaml)
        {
            var images = XamlConvertCanvas(xaml);
            for (int i = 0; i < images.Length; i++)
                DrawingField.Children.Add(images[i]);
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            _model.ClearCanvas();
        }
    }
}
