using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace QinDevilCommon.Image {
    public class ImageFormatConverser {
        public static bool BitmapToJpeg(Bitmap bmp, Stream stream, long quality) {
            using (EncoderParameters eps = new EncoderParameters(1)) {
                using (EncoderParameter ep = new EncoderParameter(Encoder.Quality, quality)) {
                    eps.Param[0] = ep;
                    ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
                    foreach (ImageCodecInfo codec in codecs) {
                        if (codec.FormatID.Equals(ImageFormat.Jpeg.Guid)) {
                            bmp.Save(stream, codec, eps);
                            return true;
                        }
                    }
                    bmp.Save(stream, ImageFormat.Bmp);
                    return false;
                }
            }
        }
    }
}
