using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
using System.Windows.Threading;

namespace CpuGpuGraph
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // gui friendly timer. "DispatcherPriority.Render" for smoother rendering
        public static DispatcherTimer Timer = new DispatcherTimer(DispatcherPriority.Render);

        // get cpu load in %
        public static PerformanceCounter TheCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        
        // event handler for datatrigger events in xaml
        public event PropertyChangedEventHandler PropertyChanged;
        
        // init value for window status. maybe read window status directly in the future
        private bool _windowMaximized = false;
        
        // init list for cpu data
        public List<int> CpuHist = new List<int>();
        
        // init list for gpu data
        public List<int> GpuHist = new List<int>();
        
        // Refresh rate per second
        const double RefreshRate = 2.0;
        public int DataPoints = new int();

        // Load gradient color LUT
        private static readonly List<string> GradientLUT = new List<string>(new string[]
        {
            "#1E90FF", "#208DFD", "#238AFC", "#2687FB", "#2984FA", "#2C81F9", "#2F7EF8", "#327BF7", "#3478F6", "#3776F5",
            "#3A73F4", "#3D70F3", "#406DF2", "#436AF1", "#4667F0", "#4864EF", "#4B61EE", "#4E5FED", "#515CEC", "#5459EB",
            "#5756EA", "#5A53E9", "#5C50E8", "#5F4DE7", "#624AE6", "#6548E5", "#6845E4", "#6B42E3", "#6E3FE2", "#703CE1",
            "#7339E0", "#7636DF", "#7933DE", "#7C30DD", "#7F2EDC", "#822BDB", "#8428DA", "#8725D9", "#8A22D8", "#8D1FD7",
            "#901CD6", "#9319D5", "#9617D4", "#9814D3", "#9B11D2", "#9E0ED1", "#A10BD0", "#A408CF", "#A705CE", "#AA02CD",
            "#AD00CC", "#AD00C7", "#AE01C3", "#AE01BF", "#AF02BB", "#B002B7", "#B003B3", "#B104AF", "#B104AB", "#B205A7",
            "#B305A3", "#B3069F", "#B4069B", "#B50796", "#B50892", "#B6088E", "#B6098A", "#B70986", "#B80A82", "#B80B7E",
            "#B90B7A", "#BA0C76", "#BA0C72", "#BB0D6E", "#BB0D6A", "#BC0E66", "#BD0F61", "#BD0F5D", "#BE1059", "#BE1055",
            "#BF1151", "#C0114D", "#C01249", "#C11345", "#C21341", "#C2143D", "#C31439", "#C31535", "#C41630", "#C5162C",
            "#C51728", "#C61724", "#C71820", "#C7181C", "#C81918", "#C81A14", "#C91A10", "#CA1B0C", "#CA1B08", "#CB1C04",
            "#CC1D00"
        });
        // init value for window status. maybe read window status directly in the future
        private bool _glow = true;

        // Graph
        PathGeometry pathGeoCpu = new PathGeometry();
        PathGeometry pathGeoGpu = new PathGeometry();
        PathFigure pathFigCpu = new PathFigure();
        PathFigure pathFigGpu = new PathFigure();
        //Path path = new Path(); // path styling in xaml
        int CanvasWidthCpu = new int();
        int CanvasWidthGpu = new int();
        int CanvasHeightCpu = new int();
        int CanvasHeightGpu = new int();

        // Main Window
        public MainWindow()
        {
            InitializeComponent();
            //GetGpuLoad();
        }
        // dll with code from http://eliang.blogspot.de/2011/05/getting-nvidia-gpu-usage-in-c.html
        [DllImport("nvGpuLoad_x86.dll")]
        public static extern int getGpuLoad();
        static int GetGpuLoad()
        {
            int a = new int();
            a = getGpuLoad();
            return a;
        }

        //Property change notifier
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // boolean: _windowMaximized - trigger porperty change if bool is changed
        public bool WindowMaximized
        {
            get { return _windowMaximized; }
            set
            {
                _windowMaximized = value;
                OnPropertyChanged();
            }
        }

        // executed after window loading
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitCpu();
            InitGpu();

            // start refresh timer
            Timer.Interval = TimeSpan.FromSeconds(1 / RefreshRate);
            Timer.IsEnabled = true;
            Timer.Start();
        }

        private void InitGpu()
        {
            CanvasWidthGpu = (int)GpuGraphCanvas.ActualWidth;
            CanvasHeightGpu = (int)GpuGraphCanvas.ActualHeight;
            DataPoints = CanvasWidthGpu / 2;

            // init gpu hist list with zeros
            for (int i = 0; i <= DataPoints; i++)
            {
                GpuHist.Add(CanvasHeightGpu);
            }
            // init graph
            pathGeoGpu.FillRule = FillRule.Nonzero;
            pathFigGpu.StartPoint = new Point(-10, CanvasHeightGpu);
            pathFigGpu.Segments = new PathSegmentCollection();
            pathGeoGpu.Figures.Add(pathFigGpu);

            double t = 0;
            foreach (int val in GpuHist)
            {
                LineSegment lineSegment = new LineSegment();
                lineSegment.Point = new Point((int)t, val);
                pathFigGpu.Segments.Add(lineSegment);
                t += (double)CanvasWidthGpu / DataPoints;
            }
            // closing graph
            LineSegment lineSegmentEnd = new LineSegment();
            lineSegmentEnd.Point = new Point(CanvasWidthGpu + 10, CanvasHeightGpu);
            pathFigGpu.Segments.Add(lineSegmentEnd);

            GpuGraph.Data = pathGeoGpu;

            // add update to timer
            Timer.Tick += UpdateGpuGraph;
        }

        private void UpdateGpuGraph(object sender, EventArgs e)
        {
            int gpuLoadValue = GetGpuLoad();
            GpuHist.RemoveAt(0);
            GpuHist.Add(CanvasHeightGpu / 100 * (100 - gpuLoadValue));

            double t = 0;
            pathFigGpu.Segments.Clear();
            foreach (int val in GpuHist)
            {
                LineSegment lineSegment = new LineSegment();
                lineSegment.Point = new Point((int)t, val);
                pathFigGpu.Segments.Add(lineSegment);
                t += (double)CanvasWidthGpu / DataPoints;
            }
            // closing graph
            LineSegment lineSegmentEnd = new LineSegment();
            lineSegmentEnd.Point = new Point(CanvasWidthGpu + 10, CanvasHeightGpu);
            pathFigGpu.Segments.Add(lineSegmentEnd);
        }

        private void UpdateGlowIndicatorCpu(int load)
        {
            Brush loadBrush = (Brush) (new BrushConverter().ConvertFromString(GradientLUT[load]));
            this.FrameAccent.BorderBrush = loadBrush;
            this.FrameBlur.BorderBrush = loadBrush;
        }

        private void InitCpu()
        {
            CanvasWidthCpu = (int)CpuGraphCanvas.ActualWidth;
            CanvasHeightCpu = (int)CpuGraphCanvas.ActualHeight;
            DataPoints = CanvasWidthCpu / 2;

            // init cpi hist list with zeros
            for (int i = 0; i <= DataPoints; i++)
            {
                CpuHist.Add(CanvasHeightCpu);
            }
            // init graph
            pathGeoCpu.FillRule = FillRule.Nonzero;
            pathFigCpu.StartPoint = new Point(-10, CanvasHeightCpu);
            pathFigCpu.Segments = new PathSegmentCollection();
            pathGeoCpu.Figures.Add(pathFigCpu);

            double t = 0;
            foreach (int val in CpuHist)
            {
                LineSegment lineSegment = new LineSegment();
                lineSegment.Point = new Point((int)t, val);
                pathFigCpu.Segments.Add(lineSegment);
                t += (double)CanvasWidthCpu / DataPoints;
            }
            // closing graph
            LineSegment lineSegmentEnd = new LineSegment();
            lineSegmentEnd.Point = new Point(CanvasWidthCpu + 10, CanvasHeightCpu);
            pathFigCpu.Segments.Add(lineSegmentEnd);

            //path.StrokeThickness = 1;         // path styling in xaml
            //path.Stroke = Brushes.Black;      // path styling in xaml
            //path.Fill = Brushes.Gray;         // path styling in xaml
            //path.Data = pathGeo;              // path styling in xaml
            CpuGraph.Data = pathGeoCpu;

            //CpuGraphCanvas.Children.Add(path);// path styling in xaml

            // start refresh timer
            Timer.Tick += UpdateCpuGraph;
        }
        private void UpdateCpuGraph(object sender, EventArgs e)
        {
            // Graph
            int cpuLoadValue = (int)Math.Ceiling(TheCpuCounter.NextValue());
            CpuHist.RemoveAt(0);
            CpuHist.Add(CanvasHeightCpu / 100 * (100 - cpuLoadValue));

            double t = 0;
            pathFigCpu.Segments.Clear();
            foreach (int val in CpuHist)
            {
                LineSegment lineSegment = new LineSegment();
                lineSegment.Point = new Point((int)t, val);
                pathFigCpu.Segments.Add(lineSegment);
                t += (double)CanvasWidthCpu / DataPoints;
            }
            // closing graph line segments
            LineSegment lineSegmentEnd = new LineSegment();
            lineSegmentEnd.Point = new Point(CanvasWidthCpu + 10, CanvasHeightCpu);
            pathFigCpu.Segments.Add(lineSegmentEnd);

            // Update glow color if activated
            if (_glow)
            {
                UpdateGlowIndicatorCpu(cpuLoadValue);
            }
        }

        // Rectangle to move borderless window
        private void rectangle1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        // Close Button
        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Minimize Button
        private void buttonMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Maximize and restore to normal Button
        private void buttonMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowMaximized)
            {
                this.WindowState = WindowState.Normal;
                WindowMaximized = false;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                WindowMaximized = true;
            }
        }

        private void ToggleGlow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                _glow = true;
                Border border = this.FindName("FrameAccent") as Border;
                border.BorderBrush = Brushes.DodgerBlue;
                border = this.FindName("FrameBlur") as Border;
                border.BorderBrush = Brushes.DodgerBlue;
            }
            else
            {
                _glow = false;
                Border border = this.FindName("FrameAccent") as Border;
                border.BorderBrush = Brushes.Transparent;
                border = this.FindName("FrameBlur") as Border;
                border.BorderBrush = Brushes.Transparent;
            }
        }

        private void Button_test_OnClick(object sender, RoutedEventArgs e)
        {
            Timer.Interval = TimeSpan.FromSeconds(0.2);
        }

        private void ComboBoxUpdateRate_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ComboBoxItem pollingTime = this.ComboBoxUpdateRate.SelectedItem as ComboBoxItem;
            //string foo = pollingTime.Content as string;
            string pollingTime = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;
            Timer.Interval = TimeSpan.FromSeconds(double.Parse(pollingTime, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
