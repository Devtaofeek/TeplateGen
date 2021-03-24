using System.Collections.Generic;

namespace ConsoleApp1
{
	public class Template
	{
		public List<TextElement> TextElements { get; set; } = new List<TextElement>();

		public List<ImageElement> ImageElements { get; set; } = new List<ImageElement>();

		public List<ShapeElement> ShapeElements { get; set; } = new List<ShapeElement>();

		public int Width { get; set; }

		public int Height { get; set; }
	}
}