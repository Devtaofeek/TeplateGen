using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConsoleApp1
{
    public class TextElement : Element
    {
        public string Text { get; set; }

        public int FontSize { get; set; }

        [Required]
        public string Font { get; set; }

        public bool IsCustomFont { get; set; }

        public List<byte> BackgroundColorRBG { get; set; }
        public byte BCR { get => BackgroundColorRBG[0]; }
        public byte BCG { get => BackgroundColorRBG[1]; }
        public byte BCB { get => BackgroundColorRBG[2]; }

        public List<byte> ColorRGB { get; set; }
        public byte CR { get => ColorRGB[0]; }
        public byte CG { get => ColorRGB[1]; }
        public byte CB { get => ColorRGB[2]; }

        public int FontWeight { get; set; }

        public bool isBold { get; set; }

        public bool IsItalic { get; set; }

        public bool isUnderLine { get; set; }

        public bool isVertical { get; set; }
    }
}