using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace QinDevilCommon.ColorClass {
    public class AYUVColor {
        public int A { get; }
        public double Y { get; }
        public double U { get; }
        public double V { get; }
        public double MaxY = 255;
        public double MinY = 0;
        public double MaxU = 111.180;
        public double MinU = -111.180;
        public double MaxV = 156.825;
        public double MinV = -156.825;
        private AYUVColor(int A, double Y, double U, double V) {
            this.A = A;
            this.Y = Y;
            this.U = U;
            this.V = V;
        }
        public static AYUVColor FromAYUV(int A, double Y, double U, double V) {
            return new AYUVColor(A, Y, U, V);
        }
        public static AYUVColor FromYUV(double Y, double U, double V) {
            return new AYUVColor(255, Y, U, V);
        }
        public ARGBColor ToARGBColor() {
            return ARGBColor.FromARGB(A, (int)Math.Round(Y + (1.14 * V)), (int)Math.Round(Y - (0.39 * U) - (0.58 * V)), (int)Math.Round(Y + (2.03 * U)));
        }
        public double GetVariance(AYUVColor color) {
            return Math.Pow(A - color.A, 2) + Math.Pow(Y - color.Y, 2) + Math.Pow(U - color.U, 2) + Math.Pow(V - color.V, 2);
        }
    }
}
