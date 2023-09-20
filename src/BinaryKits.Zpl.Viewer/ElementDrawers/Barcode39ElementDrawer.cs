using BarcodeStandard;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;
using SkiaSharp;
using System;
using System.Drawing;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    public class Barcode39ElementDrawer : BarcodeDrawerBase
    {
        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcode39;
        }

        ///<inheritdoc/>
        public override void Draw(ZplElementBase element)
        {
            Draw(element, new DrawerOptions());
        }

        ///<inheritdoc/>
        public override void Draw(ZplElementBase element, DrawerOptions options)
        {
            if (element is ZplBarcode39 barcode)
            {
                float x = barcode.PositionX;
                float y = barcode.PositionY;

                var content = barcode.Content;
                var interpretation = string.Format("*{0}*", content.Trim('*'));

                float labelFontSize = Math.Min(barcode.ModuleWidth * 7.2f, 72f);
                var labelTypeFace = options.FontLoader("A");
                var labelFont = new SKFont(labelTypeFace, labelFontSize);

                // Get font metrics
                var fontMetrics = new SKFontMetrics();
                labelFont.GetFontMetrics(out fontMetrics);

                // Retrieve font height from metrics - rounded up
                int labelHeight = barcode.PrintInterpretationLine ? (int) (fontMetrics.Ascent + fontMetrics.Descent) : 0;
                int labelHeightOffset = barcode.PrintInterpretationLineAboveCode ? (int) labelHeight : 0;

                var barcodeElement = new Barcode
                {
                    BarWidth = barcode.ModuleWidth,
                    BackColor = SkiaSharp.SKColors.Transparent,
                    Height = barcode.Height + labelHeight,
                    IncludeLabel = barcode.PrintInterpretationLine,
                    // LabelPosition = barcode.PrintInterpretationLineAboveCode ? LabelPositions.TOPCENTER : LabelPositions.BOTTOMCENTER,
                    LabelFont = labelFont,
                    AlternateLabel = interpretation
                };

                using var image = barcodeElement.Encode(BarcodeStandard.Type.Code39Extended, content);
                this.DrawBarcode(this.ConvertSKImageToByteArray(image), barcode.Height, image.Width, barcode.FieldOrigin != null, x, y, labelHeightOffset, barcode.FieldOrientation);
            }
        }
    }
}
