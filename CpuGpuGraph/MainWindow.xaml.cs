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
using System.Net;
using System.Net.NetworkInformation;

namespace CpuGpuGraph
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // gui friendly timer. "DispatcherPriority.Render" for smoother rendering
        public static DispatcherTimer Timer = new DispatcherTimer(DispatcherPriority.Render);
        public static DispatcherTimer TimerPing = new DispatcherTimer(DispatcherPriority.Render);

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

        // init list for ping data
        public List<int> PingHist = new List<int>();

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
        // ping bool
        private bool _ping = true;

        // Graph
        PathGeometry pathGeoCpu = new PathGeometry();
        PathGeometry pathGeoGpu = new PathGeometry();
        PathGeometry pathGeoPing = new PathGeometry();
        PathFigure pathFigCpu = new PathFigure();
        PathFigure pathFigGpu = new PathFigure();
        PathFigure pathFigPing = new PathFigure();
        //Path path = new Path(); // path styling in xaml
        int CanvasWidthCpu = new int();
        int CanvasWidthGpu = new int();
        int CanvasWidthPing = new int();
        int CanvasHeightCpu = new int();
        int CanvasHeightGpu = new int();
        int CanvasHeightPing = new int();

        // Ping instance
        CheckPing checkPing = new CheckPing();

        // Main Window
        public MainWindow()
        {
            // set saved position
            // disabled since I have no working check for changed display arrangement
/*            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;

            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
            MoveIntoView();*/

            InitializeComponent();
            //GetGpuLoad();
        }

        // Handle error loading nvGpuLoad_x86.dll (e.g. missing Microsoft Visual C++ 2015 Redistributable  (x86) or missing dll)
        class NvGpuLoad
        {
            // dll with code from http://eliang.blogspot.de/2011/05/getting-nvidia-gpu-usage-in-c.html
            [DllImport("nvGpuLoad_x86.dll")]
            public static extern int getGpuLoad();
            internal static int GetGpuLoad()
            {
                int a = new int();
                a = getGpuLoad();
                return a;
            }
        }

        public int GetGpuLoad()
        {
            try
            {
                return NvGpuLoad.GetGpuLoad();
            }
            catch (DllNotFoundException e)
            {
                return 0;
            }
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
            InitPing();

            // start refresh timer
            Timer.Interval = TimeSpan.FromSeconds(1 / RefreshRate);
            Timer.IsEnabled = true;
            Timer.Start();
            // start ping timer
            TimerPing.Interval = TimeSpan.FromSeconds(2);
            TimerPing.IsEnabled = true;
            TimerPing.Start();
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

            // init cpu hist list with zeros
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

        // Get Ping
        // Ping
        private void TogglePing_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                _ping = true;
            }
            else
            {
                _ping = false;
            }
        }

        private void InitPing()
        {
            CanvasWidthPing = (int)PingGraphCanvas.ActualWidth;
            CanvasHeightPing = (int)PingGraphCanvas.ActualHeight;
            DataPoints = CanvasWidthPing / 2;

            // init ping hist list with zeros
            for (int i = 0; i <= DataPoints; i++)
            {
                PingHist.Add(CanvasHeightPing);
            }
            // init graph
            pathGeoPing.FillRule = FillRule.Nonzero;
            pathFigPing.StartPoint = new Point(-10, CanvasHeightPing);
            pathFigPing.Segments = new PathSegmentCollection();
            pathGeoPing.Figures.Add(pathFigPing);

            double t = 0;
            foreach (int val in PingHist)
            {
                LineSegment lineSegment = new LineSegment();
                lineSegment.Point = new Point((int)t, val);
                pathFigPing.Segments.Add(lineSegment);
                t += (double)CanvasWidthPing / DataPoints;
            }
            // closing graph
            LineSegment lineSegmentEnd = new LineSegment();
            lineSegmentEnd.Point = new Point(CanvasWidthPing + 10, CanvasHeightPing);
            pathFigPing.Segments.Add(lineSegmentEnd);

            PingGraph.Data = pathGeoPing;

            // add update to timer
            TimerPing.Tick += UpdatePingGraph;
        }

        private void UpdatePingGraph(object sender, EventArgs e)
        {
            int pingtime = 0;
            if (_ping)
            {
                string pingValue = checkPing.GetPing();
                PingTime.Content = pingValue;

                if (!Int32.TryParse(pingValue, out pingtime))
                {
                    pingtime = 100;
                }
                if (pingtime > 100)
                {
                    pingtime = 100;
                }
            }
            else
            {
                PingTime.Content = " ";
                pingtime = -100;
            }

            PingHist.RemoveAt(0);
            PingHist.Add((int)(CanvasHeightPing * 0.01 * (100 - pingtime)));

            double t = 0;
            pathFigPing.Segments.Clear();
            foreach (int val in PingHist)
            {
                LineSegment lineSegment = new LineSegment();
                lineSegment.Point = new Point((int)t, val);
                pathFigPing.Segments.Add(lineSegment);
                t += (double)CanvasWidthPing / DataPoints;
            }
            // closing graph
            LineSegment lineSegmentEnd = new LineSegment();
            lineSegmentEnd.Point = new Point(CanvasWidthPing + 10, CanvasHeightPing);
            pathFigPing.Segments.Add(lineSegmentEnd);
        }


        public class CheckPing
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            // Create a buffer of 32 bytes of data to be transmitted.
            byte[] buffer = Encoding.ASCII.GetBytes("lololololololololololololololo11");
            int timeout = 1000;
            private string server = "8.8.8.8";

            public CheckPing()
            {
                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = false;
            }

            public string GetPing()
            {
                PingReply reply;
                try
                {
                    reply = pingSender.Send(server, timeout, buffer, options);
                    //reply = pingSender.Send(server);
                }
                catch
                {
                    return "n/a";
                }

                if (reply.Status == IPStatus.Success)
                {
/*                    Console.WriteLine("Address: {0}", reply.Address.ToString());
                    Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                    Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                    Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                    Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);*/
                    return reply.RoundtripTime.ToString();
                }
                else
                {
/*                    Console.WriteLine(reply.Status);*/
                    return ">500";
                }
            }
        }

        // Restore window size an position

        // Make sure window is in display (in case display settings changed in the mean time
        // Not working for multi monitor setup, needs rework.
        public void MoveIntoView()
        {
            if (this.Top + this.Height / 2 > System.Windows.SystemParameters.VirtualScreenHeight)
            {
                this.Top = System.Windows.SystemParameters.VirtualScreenHeight - this.Height;
            }

            if (this.Left + this.Width / 2 > System.Windows.SystemParameters.VirtualScreenWidth)
            {
                this.Left = System.Windows.SystemParameters.VirtualScreenWidth - this.Width;
            }

            if (this.Top < 0)
            {
                this.Top = 0;
            }

            if (this.Left < 0)
            {
                this.Left = 0;
            }
        }

        // Save current settings
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Maximized = false;
            }

            Properties.Settings.Default.Save();
        }

        private void OnLocationChange(object sender, EventArgs e)
        {

/*            Console.WriteLine("VirtualScreenTop: {0}", System.Windows.SystemParameters.VirtualScreenTop);
            Console.WriteLine("VirtualScreenLeft: {0}", System.Windows.SystemParameters.VirtualScreenLeft);
            Console.WriteLine("VirtualScreenWidth: {0}", System.Windows.SystemParameters.VirtualScreenWidth);
            Console.WriteLine("VirtualScreenHeight: {0}", System.Windows.SystemParameters.VirtualScreenHeight);
            Console.WriteLine("PrimaryScreenWidth: {0}", System.Windows.SystemParameters.PrimaryScreenWidth);
            Console.WriteLine("PrimaryScreenHeight: {0}", System.Windows.SystemParameters.PrimaryScreenHeight);
            Console.WriteLine("Top: {0}", this.Top);
            Console.WriteLine("Left: {0}", this.Left);
            Console.WriteLine("Height: {0}", this.Height);
            Console.WriteLine("Width: {0}", this.Width);*/
        }




    }
}
