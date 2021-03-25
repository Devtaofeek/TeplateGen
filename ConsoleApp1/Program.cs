using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	static class Program
	{
		private static string defaultFont = "Algerian";
		static async Task Main(string[] args)
		{
			var products = GenerateListOfProducts(30);
			var template = new Template
			{
				ImageElements = new List<ImageElement>()
				{
					new ImageElement
					{
						Y = 0,
						X = 0,
						Z_Index = 30,
						Height = 200,
						Width = 200,
						Opacity = 0.75M,
						ImageUrl = @"https://wiki.b-zone.ro/images/1/16/Discount_logo.png"
					}
				},
				TextElements = new List<TextElement>
				{
					 new TextElement
					 {
						  Height = 50,
						  Width = 300,
						  FontSize = 20,
						  FontWeight = 3,
						  Z_Index = 1,
						  IsItalic = false,
						  isBold = true,
						  Opacity = 1,
						  X = 260,
						  Y = 390,
						  ColorRGB = new List<byte>{ 255,255,255,255},
						  BackgroundColorRGBA = new List<byte>{8, 6,6, 0},
						  Font = "Raleway",
						  IsCustomFont = true,
						  Text = @"TODAY ONLY AT {Price}{Currency}"
					 },
					 new TextElement
					 {
						  Height = 100,
						  Width = 300,
						  FontSize = 35,
						  FontWeight = 3,
						  Z_Index = 2,
						  IsItalic = false,
						  isUnderLine = true,
						  isBold = true,
						  Opacity = 1,
						  X = 260,
						  Y = 250,
						  ColorRGB = new List<byte>{255,255,255,255},
						  BackgroundColorRGBA = new List<byte>{205, 228, 37, 215},
						  Font = "Raleway",
						  IsCustomFont = true,
						  Text = "SHOP FOR {Name} NOW! ",
						  Degree = 77,
					 },
				},
				Height = 700,
				Width = 700
			};

			var stopwatch = Stopwatch.StartNew();
			var orderedTemplateElements = GetOrderedTemplateElements(template);
			await GenerateTemplateImages(products, orderedTemplateElements, template, 10, 5);
			stopwatch.Stop();
			Console.WriteLine(new TimeSpan(stopwatch.ElapsedTicks).TotalSeconds);

			Console.ReadLine();
		}

		private static List<Element> GetOrderedTemplateElements(Template template)
		{
			var templateElements = new List<Element>();
			templateElements.AddRange(template.ImageElements);
			templateElements.AddRange(template.TextElements);
			templateElements.AddRange(template.ShapeElements);
			return templateElements.OrderBy(e => e.Z_Index).ToList();
		}

		private static async Task GenerateTemplateImages(List<Product> products, List<Element> orderedTemplate, Template template, int maximimBatchCount, int minimumBatchSize)
		{
			if (products == null || products.Count == 0)
			{
				return;
			}

			var totalItemCount = products.Count;
			var directoryName = $"output-{DateTime.UtcNow}".Replace(":", "_");
			System.IO.Directory.CreateDirectory(directoryName);

			var tasks = new List<Task>();
			var batchsize = GetAppropriateBatchSize(products, maximimBatchCount, minimumBatchSize, totalItemCount);

			var iteration = 0;
			do
			{
				var batch = GetBatch(products, iteration, batchsize);
				var batchId = iteration;
				tasks.Add(Task.Run(() => GenerateImages(orderedTemplate, template, batch, batchId + 1, directoryName)));
				iteration++;
			}
			while (totalItemCount > (batchsize * iteration));

			await Task.WhenAll(tasks);
		}

		private static int GetAppropriateBatchSize(List<Product> products, int maximimBatchCount, int minimumBatchSize, int totalItemCount)
		{
			int estimatedBatchCount = totalItemCount / minimumBatchSize;
			int appropriateBatchSize = minimumBatchSize;
			if (estimatedBatchCount > maximimBatchCount)
			{
				appropriateBatchSize = (totalItemCount / maximimBatchCount) + (totalItemCount % maximimBatchCount == 0 ? 0 : 1);
			}
			return appropriateBatchSize;
		}

		private static List<Product> GetBatch(List<Product> products, int iteration, int appropriateBatchSize)
		{
			return products.Skip(iteration * appropriateBatchSize).Take(appropriateBatchSize).ToList();
		}

		private static List<Product> GenerateListOfProducts(int count)
		{
			var listofItems = new List<Product>();
			for (int i = 0; i < count; i++)
			{
				var createdProduct = new Product
				{
					Currency = "USD",
					Name = $"Product- {i}",
					Price = new Random().Next(50, 2000),
					ImageUrl = "https://picsum.photos/700"
				};
				listofItems.Add(createdProduct);
			}

			return listofItems;
		}

		public static async Task GenerateImages(List<Element> orderdTemplateElements, Template template, List<Product> productBatch, int batchId, string directoryName)
		{
			Console.WriteLine($"Generating images for batch {batchId} : {productBatch.Count()} products");

			foreach (var product in productBatch)
			{
				using (Image canvas = new Image<Rgba32>(template.Width, template.Height))
				{
					Console.WriteLine($"Batch:{batchId} - Product:{product.Name} - generating image");

					await LayerProductImage(product.ImageUrl, canvas, template);
					foreach (var element in orderdTemplateElements)
					{
						if (element is ImageElement imageElement)
						{
							LayerImage(canvas, imageElement);
						}

						if (element is TextElement textElement)
						{
							LayerText(product, canvas, textElement);
						}
					}

					canvas.Save($"{directoryName}/{product.Name}.jpeg");
				}
			}

			Console.WriteLine($"Batch {batchId} completed");
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

		private static void LayerImage(Image canvas, ImageElement imageElement)
		{
			var flipMode = FlipMode.None;
			if (imageElement.IsFlipped)
			{
				flipMode = (FlipMode)imageElement.FlipMode;
			}
			var image = DownloadImageFromUrl(imageElement.ImageUrl).Result;
			image.Mutate(x => x.Resize(imageElement.Width, imageElement.Height)
				.Opacity((float)(imageElement.Opacity))
				.Rotate((float)imageElement.Degree)
				.Flip(flipMode));

			canvas.Mutate(ctx => ctx.DrawImage(image, new Point(imageElement.X, imageElement.Y), 1));
		}

		private static void LayerText(Product product, Image canvas, TextElement textElement)
		{
			var productJObject = JObject.FromObject(product);

			var dynamicText = BuildDynamicText(productJObject, textElement.Text);

			var textBackground = new Image<Rgba32>(textElement.Width, textElement.Height);
			var font = GetFont(textElement);

			var textGraphicsOptions = new TextGraphicsOptions()
			{
				TextOptions = {
								WrapTextWidth = textElement.Width,
								HorizontalAlignment = HorizontalAlignment.Center },
				GraphicsOptions = { }
			};

			if (textElement.BackgroundColorRGBA != null)
			{
				textBackground.Mutate(a => a.Fill(Color.FromRgba(textElement.BCR, textElement.BCG, textElement.BCB, textElement.BCA)));
			}
			textBackground.Mutate(a => a.DrawText(textGraphicsOptions, dynamicText, font, Color.FromRgb(textElement.CR, textElement.CG, textElement.CB), new PointF(0, 0)));

			canvas.Mutate(ctx => ctx.DrawImage(textBackground, new Point(textElement.X, textElement.Y), (float)textElement.Opacity));
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
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(imageUrl);
				if (response.IsSuccessStatusCode)
				{
					return Image.Load(await response.Content.ReadAsStreamAsync());
				}
				else
				{
					Console.WriteLine($"{imageUrl} not downloaded suscessfully");
					throw new Exception();
				}
			}
		}

		private static Font GetFont(TextElement item)
		{
			var fontStyle = FontStyle.Regular;
			if (item.isBold && item.IsItalic)
			{
				fontStyle = FontStyle.BoldItalic;
			}
			else if (item.isBold)
			{
				fontStyle = FontStyle.Bold;
			}
			else if (item.IsItalic)
			{
				fontStyle = FontStyle.Italic;
			}


			if (!item.IsCustomFont)
			{
				return SystemFonts.CreateFont(item.Font, item.FontSize, fontStyle);
			}
			else
			{
				var fontCollection = LoadCustomFonts(item.Font);

				if (fontCollection.TryFind(item.Font, out FontFamily fontFamily))
				{
					return fontFamily.CreateFont(item.FontSize, fontStyle);
				}
				else
				{
					return SystemFonts.CreateFont(defaultFont, item.FontSize, fontStyle);
				}
			}
		}
		private static FontCollection LoadCustomFonts(string fontName)
		{
			var collection = new FontCollection();
			try
			{
				collection.Install(Path.Combine("Resources", "Fonts", $"{fontName}.ttf"));
			}
			catch
			{
			}

			return collection;
		}
	}
}