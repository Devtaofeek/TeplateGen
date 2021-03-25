using System.Collections.Generic;

namespace ConsoleApp1
{
    public class ShapeElement : Element
    {
        public string SVG { get; set; }

        public List<byte> ColorRGB { get; set; }
        public byte CR { get => ColorRGB[0]; }
        public byte CG { get => ColorRGB[1]; }
        public byte CB { get => ColorRGB[2]; }
    }
}