using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
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

namespace RobotGui
{

    public partial class MainWindow : Window
    {
        private ChannelFactory<RobotServer.IRobotService> _factory;
        private RobotServer.IRobotService _channel;

        private readonly string _apiKey = "KEY_CLIENT_1_123";
        private readonly byte[] _sharedKey = Encoding.UTF8.GetBytes("ThisIsA16ByteKey");

        private int _x = 2, _y = 2, _rot = 0;

        public MainWindow()
        {
            InitializeComponent();
            BuildGrid();
            InitChannel(); 
            DrawRobot();    
        }

        private void InitChannel()
        {
            string serviceAddress = "http://localhost:8000/RobotService";
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(serviceAddress);
            _factory = new ChannelFactory<RobotServer.IRobotService>(binding, endpoint);
            _channel = _factory.CreateChannel();
        }

        private void BuildGrid()
        {
            Cells.Children.Clear();
            for (int i = 0; i < 25; i++)
            {
                var cell = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.White,
                    Margin = new Thickness(1)
                };
                Cells.Children.Add(cell);
            }
        }

        private void DrawRobot()
        {
            foreach (var child in Cells.Children.OfType<Border>())
                child.Background = Brushes.White;

            int idx = (_y * 5) + _x;
            if (idx >= 0 && idx < Cells.Children.Count)
            {
                var cell = (Border)Cells.Children[idx];

                cell.Background = Brushes.LightSkyBlue;

                var marker = new Grid();
                var ellipse = new Ellipse { Width = 20, Height = 20, Stroke = Brushes.DarkBlue, StrokeThickness = 2 };
                var rotText = new TextBlock
                {
                    Text = (_rot).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold
                };
                marker.Children.Add(ellipse);
                marker.Children.Add(rotText);
                cell.Child = marker;
            }

            TxtX.Text = _x.ToString();
            TxtY.Text = _y.ToString();
            TxtRot.Text = _rot.ToString();
        }

        private void UpdateFromResponse(RobotServer.OperationResult res, string sentCmd)
        {
            if (res == null)
            {
                TxtMsg.Foreground = Brushes.DarkRed;
                TxtMsg.Text = "No response.";
                return;
            }

            if (res.State != null)
            {
                _x = res.State.X;
                _y = res.State.Y;
                _rot = res.State.RotationDeg;
            }

            TxtMsg.Foreground = res.Success ? Brushes.DarkGreen : Brushes.DarkRed;
            TxtMsg.Text = $"{sentCmd}: {(res.Success ? "OK" : "FAIL")} – {res.Message}";
            DrawRobot();
        }

        private void Send(string command)
        {
            try
            {
                byte[] enc = RobotServer.AesHelper.Encrypt(command, _sharedKey);
                string base64 = Convert.ToBase64String(enc);
                var msg = new RobotServer.CommandMessage { ApiKey = _apiKey, EncryptedPayloadBase64 = base64 };
                var res = _channel.SendCommand(msg);
                UpdateFromResponse(res, command);
            }
            catch (Exception ex)
            {
                TxtMsg.Foreground = Brushes.DarkRed;
                TxtMsg.Text = $"Error: {ex.Message}";
            }
        }

        private void Left_Click(object sender, RoutedEventArgs e) => Send("MOVE_LEFT");
        private void Right_Click(object sender, RoutedEventArgs e) => Send("MOVE_RIGHT");
        private void Up_Click(object sender, RoutedEventArgs e) => Send("MOVE_UP");
        private void Down_Click(object sender, RoutedEventArgs e) => Send("MOVE_DOWN");
        private void Rotate_Click(object sender, RoutedEventArgs e) => Send("ROTATE");
        private void Clear_Click(object sender, RoutedEventArgs e) { TxtMsg.Text = ""; }
    }
}
