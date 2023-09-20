using BarcodeStandard;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    public class Barcode128ElementDrawer : BarcodeDrawerBase
    {

        /// <summary>
        /// Start sequence lookups.
        /// <see href="https://supportcommunity.zebra.com/s/article/Creating-GS1-Barcodes-with-Zebra-Printers-for-Data-Matrix-and-Code-128-using-ZPL"/>
        /// </summary>
        private static readonly Dictionary<string, BarcodeStandard.Type> startCodeMap = new Dictionary<string, BarcodeStandard.Type>()
        {
            { ">9", BarcodeStandard.Type.Code128A },
            { ">:", BarcodeStandard.Type.Code128B },
            { ">;", BarcodeStandard.Type.Code128C }
        };

        private static readonly Regex startCodeRegex = new Regex(@"^(>[9:;])(.+)$", RegexOptions.Compiled);
        private static readonly Regex invalidInvocationRegex = new Regex(@"(?<!^)>[9:;]", RegexOptions.Compiled);

        // As defined in BarcodeLib.Symbologies.Code128
        private static readonly string FNC1 = Convert.ToChar(200).ToString();

        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcode128;
        }

        ///<inheritdoc/>
        public override void Draw(ZplElementBase element)
        {
            Draw(element, new DrawerOptions());
        }

        ///<inheritdoc/>
        public override void Draw(ZplElementBase element, DrawerOptions options)
        {
            if (element is ZplBarcode128 barcode)
            {
                var barcodeType = BarcodeStandard.Type.Code128B;
                // remove any start sequences not at the start of the content (invalid invocation)
                string content = invalidInvocationRegex.Replace(barcode.Content, "");
                string interpretation = content;
                if (string.IsNullOrEmpty(barcode.Mode) || barcode.Mode == "N")
                {
                    Match startCodeMatch = startCodeRegex.Match(content);
                    if (startCodeMatch.Success)
                    {
                        barcodeType = startCodeMap[startCodeMatch.Groups[1].Value];
                        content = startCodeMatch.Groups[2].Value;
                        interpretation = content;
                    }
                    // support hand-rolled GS1
                    content = content.Replace(">8", FNC1);
                    interpretation = interpretation.Replace(">8", "");
                    // TODO: support remaining escapes within a barcode
                }
                else if (barcode.Mode == "A")
                {
                    barcodeType = BarcodeStandard.Type.Code128; // dynamic
                }
                else if (barcode.Mode == "D")
                {
                    barcodeType = BarcodeStandard.Type.Code128C;
                    content = content.Replace(">8", FNC1);
                    interpretation = interpretation.Replace(">8", "");
                    if (!content.StartsWith(FNC1))
                    {
                        content = FNC1 + content;
                    }
                }
                else if (barcode.Mode == "U")
                {
                    barcodeType = BarcodeStandard.Type.Code128C;
                    content = content.PadLeft(19, '0').Substring(0, 19);
                    int checksum = 0;
                    for (int i = 0; i < 19; i++)
                    {
                        checksum += (content[i] - 48) * (i % 2 * 2 + 7);
                    }
                    interpretation = string.Format("{0}{1}", interpretation, checksum % 10);
                    content = string.Format("{0}{1}{2}", FNC1, content, checksum % 10);
                }

                float x = barcode.PositionX;
                float y = barcode.PositionY;

                float labelFontSize = Math.Min(barcode.ModuleWidth * 7.2f, 72f);
                var labelTypeFace = options.FontLoader("A");
                var labelFont = new SKFont(labelTypeFace, labelFontSize);

                // Get font metrics
                var fontMetrics = new SKFontMetrics();
                labelFont.GetFontMetrics(out fontMetrics);

                // Retrieve font height from metrics - rounded up
                int labelHeight = barcode.PrintInterpretationLine ? (int) (fontMetrics.Descent - fontMetrics.Ascent) : 0;
                int labelHeightOffset = barcode.PrintInterpretationLineAboveCode ? (int) labelHeight : 0;

                var barcodeElement = new Barcode
                {
                    BarWidth = barcode.ModuleWidth,
                    BackColor = SkiaSharp.SKColors.Transparent,
                    Height = barcode.Height - labelHeight,
                    IncludeLabel = barcode.PrintInterpretationLine,
                    LabelFont = labelFont,
                    AlternateLabel = interpretation
                };

                using var image = barcodeElement.Encode(barcodeType, content);
                this.DrawBarcode(this.ConvertSKImageToByteArray(image), barcode.Height, image.Width, barcode.FieldOrigin != null, x, y, labelHeightOffset, barcode.FieldOrientation);
            }
        }
    }
}
