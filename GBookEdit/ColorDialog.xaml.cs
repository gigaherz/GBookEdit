using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace GBookEdit.WPF
{
    /// <summary>
    /// Interaction logic for ColorDialog.xaml
    /// </summary>
    public partial class ColorDialog : Window
    {
        public static RoutedEvent ColorChangeRoutedEvent = EventManager.RegisterRoutedEvent("SelectedColorChanged", RoutingStrategy.Bubble, typeof(ColorEventHandler), typeof(ColorDialog));
        public static DependencyProperty SelectedColorProperty = DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorDialog), new PropertyMetadata(Colors.Black, OnSelectedColorChanged));

        public static Brush Color0 => new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0xff));
        public static Brush Color1 => new SolidColorBrush(Color.FromRgb(0xff, 0x68, 0x1f));
        public static Brush Color2 => new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0xff));
        public static Brush Color3 => new SolidColorBrush(Color.FromRgb(0x9a, 0xc0, 0xcd));
        public static Brush Color4 => new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0x00));
        public static Brush Color5 => new SolidColorBrush(Color.FromRgb(0xbf, 0xff, 0x00));
        public static Brush Color6 => new SolidColorBrush(Color.FromRgb(0xff, 0x69, 0xb4));
        public static Brush Color7 => new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
        public static Brush Color8 => new SolidColorBrush(Color.FromRgb(0xd3, 0xd3, 0xd3));
        public static Brush Color9 => new SolidColorBrush(Color.FromRgb(0x00, 0xff, 0xff));
        public static Brush Color10 => new SolidColorBrush(Color.FromRgb(0xa0, 0x20, 0xf0));
        public static Brush Color11 => new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xff));
        public static Brush Color12 => new SolidColorBrush(Color.FromRgb(0x8b, 0x45, 0x13));
        public static Brush Color13 => new SolidColorBrush(Color.FromRgb(0x00, 0xff, 0x00));
        public static Brush Color14 => new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
        public static Brush Color15 => new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set {
                SetValue(SelectedColorProperty, value);
            }
        }

        public bool ApplyVisible
        {
            get { return ApplyButton.Visibility == Visibility.Visible; }
            set { ApplyButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        public event ColorEventHandler? SelectedColorChanged;
        public event ColorEventHandler? Apply;

        private WriteableBitmap? _bitmapMain;
        private WriteableBitmap? _bitmapSaturation;
        private WriteableBitmap? _bitmapRed;
        private WriteableBitmap? _bitmapGreen;
        private WriteableBitmap? _bitmapBlue;
        private bool initialized = false;

        private bool updateOriginHsv;
        private bool updateReentrancySuppression = false;

        private double _hue;
        private double _value;
        private double _saturation;

        private void OnApply(object sender, RoutedEventArgs e)
        {
            Apply?.Invoke(this, new ColorEventArgs(e.RoutedEvent, SelectedColor));
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorDialog cd)
            {
                cd.OnSelectedColorChanged();
            }
        }

        private void OnSelectedColorChanged()
        {
            if (!initialized) return;

            if (!updateOriginHsv)
            {
                var (h, s, v) = HSV.ToHSV(SelectedColor.R, SelectedColor.G, SelectedColor.B);

                if (s > (1.0/255.0) && v > (1.0 / 255.0)) _hue = h;
                if (v > (1.0 / 255.0)) _saturation = s;
                _value = v;

                ColorEllipse.SetValue(Canvas.LeftProperty, _hue * GradientImage.ActualWidth / 360 - ColorEllipse.ActualWidth / 2);
                ColorEllipse.SetValue(Canvas.TopProperty, (1 - _value) * GradientImage.ActualHeight - ColorEllipse.ActualHeight / 2);

                if (v > 0)
                {
                    var sat = _saturation * SaturationSlider.Maximum;
                    if ((int)sat != (int)SaturationSlider.Value)
                    {
                        SaturationSlider.Value = sat;
                    }
                }
            }

            RedSlider.Value = SelectedColor.R;
            GreenSlider.Value = SelectedColor.G;
            BlueSlider.Value = SelectedColor.B;
            bColor.Background = new SolidColorBrush(SelectedColor);
            ColorTextBox.Text = $"{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";

            SelectedColorChanged?.Invoke(this, new ColorEventArgs(ColorChangeRoutedEvent, SelectedColor));

            UpdateMainGradient();
            UpdateSaturationGradient();
            UpdateRedGradient();
            UpdateGreenGradient();
            UpdateBlueGradient();
        }

        public ColorDialog()
        {
            InitializeComponent();

            initialized = true;

            SelectedColor = Colors.White;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RecreateMainGradient();
            RecreateSaturationGradient();
            RecreateRedGradient();
            RecreateGreenGradient();
            RecreateBlueGradient();
        }

        private void GradientImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecreateMainGradient();
        }

        private void SaturationSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecreateSaturationGradient();
        }

        private void RedSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecreateRedGradient();
        }

        private void GreenSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecreateGreenGradient();
        }

        private void BlueSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecreateBlueGradient();
        }

        private void RecreateMainGradient()
        {
            var w = (int)(GradientImage.ActualWidth != 0 ? GradientImage.ActualWidth : GradientImage.Width);
            var h = (int)(GradientImage.ActualHeight != 0 ? GradientImage.ActualHeight : GradientImage.Height);

            _bitmapMain = new WriteableBitmap(w, h, 1, 1, PixelFormats.Bgra32, null);

            GradientImage.Source = _bitmapMain;

            UpdateMainGradient();
        }

        private void RecreateSaturationGradient()
        {
            var w = (int)SaturationSlider.ContentWidth;
            var h = (int)SaturationSlider.ContentHeight;

            _bitmapSaturation = new WriteableBitmap(w, h, 1, 1, PixelFormats.Bgra32, null);

            SaturationSlider.ImageSource = _bitmapSaturation;

            UpdateSaturationGradient();
        }

        private void RecreateRedGradient()
        {
            var w = (int)RedSlider.ContentWidth;
            var h = (int)RedSlider.ContentHeight;

            _bitmapRed = new WriteableBitmap(w, h, 1, 1, PixelFormats.Bgra32, null);

            RedSlider.ImageSource = _bitmapRed;

            UpdateRedGradient();
        }

        private void RecreateGreenGradient()
        {
            var w = (int)GreenSlider.ContentWidth;
            var h = (int)GreenSlider.ContentHeight;

            _bitmapGreen = new WriteableBitmap(w, h, 1, 1, PixelFormats.Bgra32, null);

            GreenSlider.ImageSource = _bitmapGreen;

            UpdateGreenGradient();
        }

        private void RecreateBlueGradient()
        {
            var w = (int)BlueSlider.ContentWidth;
            var h = (int)BlueSlider.ContentHeight;

            _bitmapBlue = new WriteableBitmap(w, h, 1, 1, PixelFormats.Bgra32, null);

            BlueSlider.ImageSource = _bitmapBlue;

            UpdateBlueGradient();
        }

        private void UpdateMainGradient()
        {
            if (_bitmapMain == null) return;

            var stride = _bitmapMain.BackBufferStride;
            var pixels = new int[_bitmapMain.PixelHeight, _bitmapMain.PixelWidth];

            var s = SaturationSlider.Value / SaturationSlider.Maximum;

            for (int py = 0; py < _bitmapMain.PixelHeight; py++)
            {
                for (int px = 0; px < _bitmapMain.PixelWidth; px++)
                {
                    var x = px / (double)_bitmapMain.PixelWidth;
                    var y = py / (double)_bitmapMain.PixelHeight;

                    var h = Math.Max(0, Math.Min(x * 360, 360));
                    var v = 1 - y;

                    var (r, g, b) = HSV.ToRGB(h, s, v);

                    pixels[py, px] = (255 << 24) | (r << 16) | (g << 8) | b;
                }
            }

            _bitmapMain.WritePixels(new Int32Rect(0, 0, _bitmapMain.PixelWidth, _bitmapMain.PixelHeight), pixels, stride, 0);
        }

        private void UpdateSaturationGradient()
        {
            if (_bitmapSaturation == null) return;

            var stride = _bitmapSaturation.BackBufferStride;
            var pixels = new int[_bitmapSaturation.PixelHeight, _bitmapSaturation.PixelWidth];

            for (int py = 0; py < _bitmapSaturation.PixelHeight; py++)
            {
                var y = py / (double)_bitmapSaturation.PixelHeight;

                var (r, g, b) = HSV.ToRGB(_hue, 1-y, _value);

                for (int px = 0; px < _bitmapSaturation.PixelWidth; px++)
                {
                    pixels[py, px] = (255 << 24) | (r << 16) | (g << 8) | b;
                }
            }

            _bitmapSaturation.WritePixels(new Int32Rect(0, 0, _bitmapSaturation.PixelWidth, _bitmapSaturation.PixelHeight), pixels, stride, 0);
        }

        private void UpdateRedGradient()
        {
            if (_bitmapRed == null) return;

            var stride = _bitmapRed.BackBufferStride;
            var pixels = new int[_bitmapRed.PixelHeight, _bitmapRed.PixelWidth];

            for (int px = 0; px < _bitmapRed.PixelWidth; px++)
            {
                var x = px / (double)_bitmapRed.PixelWidth;

                var r = (int)(x * 255);
                var g = SelectedColor.G;
                var b = SelectedColor.B;

                for (int py = 0; py < _bitmapRed.PixelHeight; py++)
                {
                    pixels[py, px] = (255 << 24) | (r << 16) | (g << 8) | (b);
                }
            }

            _bitmapRed.WritePixels(new Int32Rect(0, 0, _bitmapRed.PixelWidth, _bitmapRed.PixelHeight), pixels, stride, 0);
        }

        private void UpdateGreenGradient()
        {
            if (_bitmapGreen == null) return;

            var stride = _bitmapGreen.BackBufferStride;
            var pixels = new int[_bitmapGreen.PixelHeight, _bitmapGreen.PixelWidth];

            for (int px = 0; px < _bitmapGreen.PixelWidth; px++)
            {
                var x = px / (double)_bitmapGreen.PixelWidth;

                var r = SelectedColor.R;
                var g = (int)(x * 255);
                var b = SelectedColor.B;

                for (int py = 0; py < _bitmapGreen.PixelHeight; py++)
                {
                    pixels[py, px] = (255 << 24) | (r << 16) | (g << 8) | (b);
                }
            }

            _bitmapGreen.WritePixels(new Int32Rect(0, 0, _bitmapGreen.PixelWidth, _bitmapGreen.PixelHeight), pixels, stride, 0);
        }

        private void UpdateBlueGradient()
        {
            if (_bitmapBlue == null) return;

            var stride = _bitmapBlue.BackBufferStride;
            var pixels = new int[_bitmapBlue.PixelHeight, _bitmapBlue.PixelWidth];

            for (int px = 0; px < _bitmapBlue.PixelWidth; px++)
            {
                var x = px / (double)_bitmapBlue.PixelWidth;

                var r = SelectedColor.R;
                var g = SelectedColor.G;
                var b = (int)(x * 255);

                for (int py = 0; py < _bitmapBlue.PixelHeight; py++)
                {
                    pixels[py, px] = (255 << 24) | (r << 16) | (g << 8) | (b);
                }
            }

            _bitmapBlue.WritePixels(new Int32Rect(0, 0, _bitmapBlue.PixelWidth, _bitmapBlue.PixelHeight), pixels, stride, 0);
        }

        private void SaturationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var s = SaturationSlider.Value / SaturationSlider.Maximum;
            var (r, g, b) = HSV.ToRGB(_hue, s, _value);
            _saturation = s;
            SetColorSuppressingReentrancy(Color.FromRgb(r, g, b), true);
        }

        private void SetColorSuppressingReentrancy(Color color, bool updatingHsv = false)
        {
            if (updateReentrancySuppression) return;
            updateOriginHsv = updatingHsv;
            updateReentrancySuppression = true;
            SelectedColor = color;
            updateReentrancySuppression = false;
            updateOriginHsv = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OnApply(this, e);
            DialogResult = true;
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            OnApply(this, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (sender as Button)!;

            if (button.Background is SolidColorBrush b)
                SelectedColor = b.Color;
        }

        private void GradientImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CalculateColorFromGradient(e.GetPosition(GradientImage));
            GradientImage.CaptureMouse();
        }

        private void GradientImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (GradientImage.IsMouseCaptured)
            {
                CalculateColorFromGradient(e.GetPosition(GradientImage));
            }
        }

        private void GradientImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            GradientImage.ReleaseMouseCapture();
        }

        private void CalculateColorFromGradient(Point point)
        {
            var x = Clamp(point.X / GradientImage.ActualWidth, 0, 1);
            var y = Clamp(point.Y / GradientImage.ActualHeight, 0, 1);

            var h = Clamp(x * 360, 0, 360);
            var s = _saturation;
            var v = 1 - y;

            var (r, g, b) = HSV.ToRGB(h, s, v);

            _hue = h;
            _value = v;

            SetColorSuppressingReentrancy(Color.FromRgb(r, g, b), true);

            ColorEllipse.SetValue(Canvas.LeftProperty, Clamp(point.X, 0, GradientImage.ActualWidth) - ColorEllipse.ActualWidth / 2);
            ColorEllipse.SetValue(Canvas.TopProperty, Clamp(point.Y, 0, GradientImage.ActualHeight) - ColorEllipse.ActualHeight / 2);
        }

        private double Clamp(double v, double min, double max)
        {
            return Math.Min(Math.Max(v, min), max);
        }

        private void RedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RedTextBox.Text = ((int)RedSlider.Value).ToString();
            var color = Color.FromRgb((byte)RedSlider.Value, SelectedColor.G, SelectedColor.B);
            SetColorSuppressingReentrancy(color);
        }

        private void RedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (byte.TryParse(RedTextBox.Text, out var value))
            {
                RedSlider.Value = value;
            }
        }

        private void GreenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            GreenTextBox.Text = ((int)GreenSlider.Value).ToString();
            var color = Color.FromRgb(SelectedColor.R, (byte)GreenSlider.Value, SelectedColor.B);
            SetColorSuppressingReentrancy(color);
        }

        private void GreenTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (byte.TryParse(GreenTextBox.Text, out var value))
            {
                GreenSlider.Value = value;
            }
        }

        private void BlueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            BlueTextBox.Text = ((int)BlueSlider.Value).ToString();
            var color = Color.FromRgb(SelectedColor.R, SelectedColor.G, (byte)BlueSlider.Value);
            SetColorSuppressingReentrancy(color);
        }

        private void BlueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (byte.TryParse(BlueTextBox.Text, out var value))
            {
                BlueSlider.Value = value;
            }
        }

        private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BookToFlow.TryParseColor(ColorTextBox.Text, out var color, requireHash: false))
            {
                SelectedColor = color;
            }
        }
    }

    public delegate void ColorEventHandler(object sender, ColorEventArgs e);

    public class ColorEventArgs : RoutedEventArgs
    {
        public Color Color { get; set; }

        public ColorEventArgs(RoutedEvent routedEvent, Color color) : base(routedEvent)
        {
            Color = color;
        }
    }
}
