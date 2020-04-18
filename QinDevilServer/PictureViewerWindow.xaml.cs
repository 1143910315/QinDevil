using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace QinDevilServer {
    /// <summary>
    /// PictureViewerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PictureViewerWindow : Window {
        private readonly GameData gameData;
        private bool mouseDown;
        private Point mouseXY;
        public PictureViewerWindow(GameData gameData) {
            InitializeComponent();
            this.gameData = gameData;
        }
        public PictureViewerWindow(UserInfo userInfo) {
            InitializeComponent();
            try {
                Uri baseUri = new Uri(System.Reflection.Assembly.GetEntryAssembly().Location);
                IMG1.Source = new BitmapImage(new Uri(baseUri, userInfo.PicPath));
                Show();
            } catch (Exception) {
                Close();
            }
        }
        private void IMG1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            ContentControl img = sender as ContentControl;
            if (img == null) {
                return;
            }
            img.CaptureMouse();
            mouseDown = true;
            mouseXY = e.GetPosition(img);
        }
        private void IMG1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            ContentControl img = sender as ContentControl;
            if (img == null) {
                return;
            }
            img.ReleaseMouseCapture();
            mouseDown = false;
        }
        private void IMG1_MouseMove(object sender, MouseEventArgs e) {
            ContentControl img = sender as ContentControl;
            if (img == null) {
                return;
            }
            if (mouseDown) {
                Domousemove(img, e);
            }
        }
        private void Domousemove(ContentControl img, MouseEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed) {
                return;
            }
            TransformGroup group = IMG.FindResource("Imageview") as TransformGroup;
            TranslateTransform transform = group.Children[1] as TranslateTransform;
            Point position = e.GetPosition(img);
            transform.X -= mouseXY.X - position.X;
            transform.Y -= mouseXY.Y - position.Y;
            mouseXY = position;
        }
        private void IMG1_MouseWheel(object sender, MouseWheelEventArgs e) {
            ContentControl img = sender as ContentControl;
            if (img == null) {
                return;
            }
            Point point = e.GetPosition(img);
            TransformGroup group = IMG.FindResource("Imageview") as TransformGroup;
            double delta = e.Delta * 0.001;
            DowheelZoom(group, point, delta);
        }
        private void DowheelZoom(TransformGroup group, Point point, double delta) {
            Point pointToContent = group.Inverse.Transform(point);
            ScaleTransform transform = group.Children[0] as ScaleTransform;
            if (transform.ScaleX + delta < 0.1) {
                return;
            }
            transform.ScaleX += delta;
            transform.ScaleY += delta;
            TranslateTransform transform1 = group.Children[1] as TranslateTransform;
            transform1.X = -1 * ((pointToContent.X * transform.ScaleX) - point.X);
            transform1.Y = -1 * ((pointToContent.Y * transform.ScaleY) - point.Y);
        }
    }
}
