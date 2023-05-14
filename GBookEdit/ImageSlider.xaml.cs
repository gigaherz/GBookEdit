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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GBookEdit.WPF
{
    /// <summary>
    /// Interaction logic for ImageSlider.xaml
    /// </summary>
    public partial class ImageSlider : UserControl
    {
        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(ImageSlider), new PropertyMetadata(null));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(ImageSlider), new PropertyMetadata(0.0, OnLayoutChanged));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(ImageSlider), new PropertyMetadata(1.0, OnLayoutChanged));
        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register("Interval", typeof(double), typeof(ImageSlider), new PropertyMetadata(0.0, OnLayoutChanged));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(ImageSlider), new PropertyMetadata(0.0, OnValueChanged));
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ImageSlider), new PropertyMetadata(Orientation.Horizontal, OnLayoutChanged));

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), typeof(ImageSlider));

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Interval
        {
            get { return (double)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set
            {
                if (value < Minimum)
                    value = Minimum;
                if (value > Maximum)
                    value = Maximum;

                if (Interval > 0)
                    value = Math.Round(value / Interval) * Interval;

                SetValue(ValueProperty, value);
            }
        }

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set
            {
                SetValue(OrientationProperty, value);
            }
        }

        public double ContentWidth => sizeGrid.ActualWidth;
        public double ContentHeight => sizeGrid.ActualHeight;

        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageSlider slider)
                return;
            slider.UpdateMarker();
            slider.RaiseEvent(new RoutedPropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue, ValueChangedEvent));
        }
        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageSlider slider)
                return;
            slider.UpdateMarker();
        }


        public ImageSlider()
        {
            InitializeComponent();
        }

        private void GradientImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            GradientImage.CaptureMouse();
            UpdatePosition(e.GetPosition(GradientImage));
        }

        private void GradientImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (GradientImage.IsMouseCaptured)
            {
                UpdatePosition(e.GetPosition(GradientImage));
            }
        }

        private void UpdatePosition(Point mouse)
        {
            if (Orientation == Orientation.Horizontal)
            {
                double range = (Maximum - Minimum);
                double pos =  mouse.X;
                double size = sizeGrid.ActualWidth;
                Value = Minimum + range * Math.Max(Math.Min(pos / size, 1), 0);
            }
            else
            {
                double range = (Maximum - Minimum);
                double pos = sizeGrid.ActualHeight - mouse.Y;
                double size = sizeGrid.ActualHeight;
                Value = Minimum + range * Math.Max(Math.Min(pos / size, 1), 0);
            }
        }

        private void GradientImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            GradientImage.ReleaseMouseCapture();
        }

        private void UpdateMarker()
        {
            if (Orientation == Orientation.Horizontal)
            {
                double pos = sizeGrid.ActualWidth * (Value - Minimum) / (Maximum - Minimum);
                Canvas.SetLeft(bMarker, pos - bMarker.ActualWidth / 2);
                Canvas.SetTop(bMarker, 0);
                bMarker.Width = 5;
                bMarker.Height = sizeGrid.ActualHeight;
            }
            else
            {
                double pos = sizeGrid.ActualHeight - sizeGrid.ActualHeight * (Value - Minimum) / (Maximum - Minimum);
                Canvas.SetLeft(bMarker, 0);
                Canvas.SetTop(bMarker, pos - bMarker.ActualHeight / 2);
                bMarker.Width = sizeGrid.ActualWidth;
                bMarker.Height = 5;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMarker();
        }
    }
}
