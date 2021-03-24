namespace ConsoleApp1
{
    public class TextElement:Element
    {
        
        public string Text { get; set; }
        public int FontSize { get; set; }

        public string FontFamily { get; set; }

        public string BackgroundColor { get; set; }
        public string Color { get; set; }

	
		
        public int FontWeight { get; set; }

        public bool isBold { get; set; }

        public bool IsItalic { get; set; }

		
        public bool isUnderLine { get;  set; }
        public bool isVertical { get; set; }
    }
}