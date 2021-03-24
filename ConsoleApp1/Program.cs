using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	static class Program
	{

		private static HttpWebRequest webClient;

		static void Main(string[] args)
		{
			TestBatchingAlgorithm();
			var product = new Product
			{
				Currency = "USD",
				Name = "Headphone",
				Price = 50,
				ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?ixid=MXwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHw%3D&ixlib=rb-1.2.1&auto=format&fit=crop&w=1920&q=1280"
			};

			var template = new Template
			{
				ImageElements = new List<ImageElement>()
			{
				 new ImageElement
				 {
					 Y = 0,
					 X = 0,
					 Height = 300,
					 Width = 300,
					 Opacity = 0.75M,
					 ImageUrl = "https://thumbs.dreamstime.com/b/discount-stamp-vector-clip-art-33305813.jpg" // change this to a PNG
				 }
			},
				TextElements = new List<TextElement>
			{
				new TextElement
				{
					Height = 250,
					Width = 300,
					FontSize = 25,
					FontWeight = 3,
					Z_Index = 1,
					IsItalic = false,
					isBold = true,
					Opacity = 1,
					X = 150,
					Y = 50,
					ColorRGB = new List<byte>{255, 0, 0 },
					Font = "Arial",
					IsCustomFont = false,
					Text = @"{Name} is sold at {Price}{Currency}. IT has been the industry's standard dummy text ever since the 1500s,when an unknown printer took a of type and scrambled it to make a type specimen book"
				},
				new TextElement
				{
					Height = 200,
					Width = 400,
					FontSize = 35,
					FontWeight = 3,
					Z_Index = 1,
					IsItalic = true,
					isUnderLine = true,
					isBold = true,
					Opacity = 0.5M,
					X = 50,
					Y = 220,
					ColorRGB = new List<byte>{255, 255, 255 },
					BackgroundColorRBG = new List<byte>{0, 120, 120 },
					Font = "Calibri",
					IsCustomFont = false,
					Text = "{Name} is the best",
				}
			},
				Height = 700,
				Width = 700
			};

			GenerateTemplate(template, product);
		}

		private static void TestBatchingAlgorithm()
		{
			var maximumBatchCount = 10;
			var maximumBatchSize = 5;
			var listofItems = GenerateListOfItems();

			var iteration = 0;
			var totalItemCount = listofItems.Count;
			int appropriateBatchSize = totalItemCount / maximumBatchSize > maximumBatchCount ? (totalItemCount / maximumBatchSize) + (totalItemCount % maximumBatchSize == 0 ? 0 : 1) : maximumBatchSize;

			

			do
			{
				foreach (var item in Batch(listofItems, iteration, appropriateBatchSize))
				{
					Console.WriteLine($" item is added to batch { iteration + 1} ");
				}

				iteration++;
			} while (totalItemCount > (appropriateBatchSize * iteration));
		}

		private static IEnumerable<int> Batch(List<int> listofItems, int iteration, int appropriateBatchSize)
		{
			return listofItems.Skip(iteration*appropriateBatchSize).Take(appropriateBatchSize);
		}

		private static List<int> GenerateListOfItems()
		{
			var listofItems = new List<int>();
			for (int i = 0; i < 10; i++)
			{
				listofItems.Add(i);
			}

			return listofItems;
		}

		public static async Task GenerateTemplate(Template template, Product product)
		{
			System.IO.Directory.CreateDirectory("output");

			using (Image canvas = new Image<Rgba32>(template.Width, template.Height))
			{
				LayerProductImage(product.ImageUrl, canvas, template);

				await LayerImages(canvas, template);

				LayerTexts(template, product, canvas);

				canvas.Save("output/wordart.png");
			}
		}

		private static async Task LayerProductImage(string productImageUrl, Image canvas, Template template)
		{
			var backgroundImage = await DownloadImageFromUrl(productImageUrl);

			backgroundImage.Mutate(x => x
								 .Resize(new ResizeOptions
								 {
									 Size = new Size(template.Width, template.Height),
									 Mode = ResizeMode.Pad,
									 Position = AnchorPositionMode.Center
								 }));
			canvas.Mutate(c => c.DrawImage(backgroundImage, 1));
		}

		private static async Task LayerImages(Image canvas, Template template)
		{
			foreach (var item in template.ImageElements)
			{
				var image = await DownloadImageFromUrl(item.ImageUrl);
				image.Mutate(x => x.Resize(item.Width, item.Height)
					.Opacity((float)(item.Opacity)));
				// rotation
				// flipped?

				canvas.Mutate(ctx => ctx.DrawImage(image, new Point(item.X, item.Y), 1)); // add Z index
			}
		}

		private static void LayerTexts(Template template, Product product, Image canvas)
		{
			var productJObject = JObject.FromObject(product);

			foreach (var textElement in template.TextElements)
			{
				var text = BuildDynamicText(productJObject, textElement.Text);

				var textBackground = new Image<Rgba32>(textElement.Width, textElement.Height);
				var font = GetFont(textElement);

				// this will come from TextElements in the future, for now leave it hardcoded
				var textGraphicsOptions = new TextGraphicsOptions()
				{
					TextOptions = {
						WrapTextWidth = textElement.Width,
						HorizontalAlignment = HorizontalAlignment.Center },
					GraphicsOptions = { }
				};

				if (textElement.BackgroundColorRBG != null)
				{
					textBackground.Mutate(a => a.Fill(Color.FromRgb(textElement.BCR, textElement.BCG, textElement.BCB)));
				}
				textBackground.Mutate(a => a.DrawText(textGraphicsOptions, text, font, Color.FromRgb(textElement.CR, textElement.CG, textElement.CB), new PointF(0, 0)));

				canvas.Mutate(ctx => ctx.DrawImage(textBackground, new Point(textElement.X, textElement.Y), (float)textElement.Opacity));
			}
		}

		private static Font GetFont(TextElement item)
		{
			// TODO: handle both bold and italic simultaneously
			var fontStyle = item.IsItalic ? FontStyle.Italic : FontStyle.Regular;
			fontStyle = item.isBold ? FontStyle.Bold : FontStyle.Regular;

			// TODO: item.FontFamily needs to be supported. (must be a valid FontFamily value)
			// TODO: handle custom fonts from user
			if (item.IsCustomFont)
			{
				// get the font from the DB, and apply it and return
			}

			return SystemFonts.CreateFont(item.Font, item.FontSize, fontStyle);
		}

		private static string BuildDynamicText(JObject productJObject, string text)
		{
			var tokens = new Dictionary<string, string>();

			Regex reg = new Regex(@"{\w+}");
			foreach (Match match in reg.Matches(text))
			{
				var propertyName = match.Value.Substring(1, match.Value.Length - 2);
				tokens.Add(match.Value, productJObject.Value<string>(propertyName));
			}

			foreach (var token in tokens)
			{
				text = text.Replace(token.Key, tokens[token.Key]);
			}

			return text;
		}

		private static async Task<Image> DownloadImageFromUrl(string imageUrl)
		{
			// we will use something similar to this in the final code
			/*using (var client = new HttpClient())
			{
				var response = await client.GetAsync(imageUrl);
				if (response.IsSuccessStatusCode)
				{
					return Image.Load(await response.Content.ReadAsStreamAsync());
				}
			}*/

			webClient = (HttpWebRequest)HttpWebRequest.Create(imageUrl);
			webClient.AllowWriteStreamBuffering = true;
			webClient.AllowReadStreamBuffering = true;
			webClient.Timeout = 30000;

			var webResponse = webClient.GetResponse();
			var stream = webResponse.GetResponseStream();
			var image = Image.Load(stream);

			return image;
		}
	}
}