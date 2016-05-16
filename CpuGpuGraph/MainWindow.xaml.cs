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
        // gui friendly sleep
        public static DispatcherTimer CpuTimer = new DispatcherTimer();
        public static DispatcherTimer GpuTimer = new DispatcherTimer();
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

        // boolean: _windowMaximized
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

            // start refresh timer
            GpuTimer.Interval = TimeSpan.FromSeconds(1 / RefreshRate);
            GpuTimer.IsEnabled = true;
            GpuTimer.Tick += UpdateGpuGraph;
            GpuTimer.Start();
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
            CpuTimer.Interval = TimeSpan.FromSeconds(1 / RefreshRate);
            CpuTimer.IsEnabled = true;
            CpuTimer.Tick += UpdateCpuGraph;
            CpuTimer.Start();
        }
        private void UpdateCpuGraph(object sender, EventArgs e)
        {
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
            // closing graph
            LineSegment lineSegmentEnd = new LineSegment();
            lineSegmentEnd.Point = new Point(CanvasWidthCpu + 10, CanvasHeightCpu);
            pathFigCpu.Segments.Add(lineSegmentEnd);
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

    }
}
