using GBookEdit.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBookEdit.WPF
{
    public static class HSV
    {
        /// <summary>
        /// Converts a color _in HSV to RGB
        /// </summary>
        /// <param name="h">Hue, _in degrees 0..360</param>
        /// <param name="s">Saturation, 0..1</param>
        /// <param name="v">Value, 0..1</param>
        /// <returns>The r,g,b components of the color</returns>
        public static (byte r, byte g, byte v) ToRGB(double h, double s, double v)
        {
            if (s == 0)
            {
                var gray = (byte)(v * 255);
                return (gray, gray, gray);
            }
            
            h = h >= 360 ? 0 : h / 60;

            var i = (int)Math.Truncate(h);
            var f = h - i;

            var p = v * (1.0 - s);
            var q = v * (1.0 - (s * f));
            var t = v * (1.0 - (s * (1.0 - f)));

            double r, g, b;

            switch (i)
            {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;

                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;

                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;

                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;

                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;

                default:
                    r = v;
                    g = p;
                    b = q;
                    break;
            }

            return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        /// <summary>
        /// Converts a color _in RGB to HSV
        /// </summary>
        /// <param name="r">The red component of the color</param>
        /// <param name="g">The green component of the color</param>
        /// <param name="b">The blue component of the color</param>
        /// <returns>(h,s,v) where h=0..360, s=0..1, and v=0..1</returns>
        public static (double h, double s, double v) ToHSV(byte r, byte g, byte b)
        {
            var min = Math.Min(Math.Min(r, g), b);
            var max = Math.Max(Math.Max(r, g), b);
            double delta = max - min;

            double v = max / 255.0;
            double s = max == 0 ? 0 : delta / max;
            double h = 0;

            if (s > (1.0/255.0))
            {
                if (r == max)
                    h = (g - b) / delta;
                else if (g == max)
                    h = 2 + (b - r) / delta;
                else if (b == max)
                    h = 4 + (r - g) / delta;

                h *= 60;

                if (h < 0.0)
                    h += 360;
            }

            return (h, s, v);
        }

    }
}
