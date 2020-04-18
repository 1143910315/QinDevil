using System;
using System.Collections.Generic;
using System.Text;

namespace QinDevilCommon.ColorClass {
    public class ARGBColor {
        public int A { get; }
        public int R { get; }
        public int G { get; }
        public int B { get; }
        private ARGBColor(int A, int R, int G, int B) {
            this.A = A;
            this.R = R;
            this.G = G;
            this.B = B;
        }
        public static ARGBColor FromARGB(int A, int R, int G, int B) {
            return new ARGBColor(A, R, G, B);
        }
        public static ARGBColor FromRGB(int R, int G, int B) {
            return new ARGBColor(255, R, G, B);
        }
        public AYUVColor ToAYUVColor() {
            return AYUVColor.FromAYUV(A, 0.299 * R + 0.587 * G + 0.114 * B, -0.147 * R - 0.289 * G + 0.436 * B, 0.615 * R - 0.515 * G - 0.100 * B);
        }
    }
}
