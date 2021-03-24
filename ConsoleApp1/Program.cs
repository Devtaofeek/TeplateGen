using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	static class Program
	{

		private static HttpWebRequest webClient;

		static async Task Main(string[] args)
		{

			var product = new Product
			{
				Currency = "USD",
				Name = "Headphone",
				Price = 50,
				ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?ixid=MXwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHw%3D&ixlib=rb-1.2.1&auto=format&fit=crop&w=1920&q=1280"
			};
			// generate 4,5,7,49,50,56,150 products,
			// the title of each product = his number in the list 1,2,3,4,5...10, print the product names from each batch ( to ensure batches are generated correctly)
			var products = GenerateListOfItems(30);
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

			await ProcessTemplateGenerationAsync(products, template, 10, 5);
			Console.ReadLine();
		}

		private static async Task ProcessTemplateGenerationAsync(List<Product> products, Template template, int maximimBatchCount, int minimumBatchSize)
		{
			
			int totalItemCount = products.Count;

			// use batchsize
			var batchsize = GetAppropriateBatchSize(products, maximimBatchCount, minimumBatchSize, totalItemCount); 

			List<Task> tasks = new List<Task>();
			var iteration = 0;
			do
			{
				var batch = Batch(products, iteration, batchsize);//rename to getbatch
				tasks.Add(Task.Factory.StartNew(() => GenerateImages(template, batch, Guid.NewGuid())));
				iteration = iteration + 1;
			} while (totalItemCount > (batchsize * iteration));

			
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

	

		private static List<Product> Batch(List<Product> products, int iteration, int appropriateBatchSize)
		{
			return products.Skip(iteration * appropriateBatchSize).Take(appropriateBatchSize).ToList();
		}

		private static List<Product> GenerateListOfItems(int count)
		{
			var listofItems = new List<Product>();
			for (int i = 0; i < count; i++)
			{
				var createdProduct =  new Product
				{
					Currency = "USD",
					Name = i.ToString(),
					Price = 50,
					ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?ixid=MXwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHw%3D&ixlib=rb-1.2.1&auto=format&fit=crop&w=1920&q=1280"
				};
				listofItems.Add(createdProduct);
			}

			return listofItems;
		}

		public static async Task GenerateImages(Template template, List<Product> productBatch, Guid BatchId)
		{
			Console.WriteLine($"Batch {BatchId} ---- Generating Images");
			System.IO.Directory.CreateDirectory("output");

			Parallel.For(0, productBatch.Count, i =>
			{
				using (Image canvas = new Image<Rgba32>(template.Width, template.Height))
				{
					LayerProductImage(productBatch[i].ImageUrl, canvas, template);

					LayerImages(canvas, template);

					LayerTexts(template, productBatch[i], canvas);


					Console.WriteLine($"Batch {BatchId} ----- with product - {productBatch[i].Name} saving generated");
					canvas.Save($"output/{productBatch[i].Name}.png");
				}

			});
		
			Console.WriteLine($"Batch {BatchId} ----- done!");
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

		private static void LayerImages(Image canvas, Template template)
		{
			Parallel.ForEach(template.ImageElements, (item) =>
			{
				var image = DownloadImageFromUrl(item.ImageUrl).Result;
				image.Mutate(x => x.Resize(item.Width, item.Height)
					.Opacity((float)(item.Opacity)));
				// rotation
				// flipped?

				canvas.Mutate(ctx => ctx.DrawImage(image, new Point(item.X, item.Y), 1)); // add Z index
			});
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

			//webClient = (HttpWebRequest)HttpWebRequest.Create(imageUrl);
			//webClient.AllowWriteStreamBuffering = true;
			//webClient.AllowReadStreamBuffering = true;
			//webClient.Timeout = 100000;

			//var webResponse = webClient.GetResponse();
			//var stream = webResponse.GetResponseStream();
			//var image = Image.Load(stream);

			//return image;
		}
	}
}