using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal static class Program
    {
        private static HttpWebRequest webClient;

        private static async Task Main(string[] args)
        {
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
                    Height = 250,
                    Width = 300,
                    FontSize = 25,
                    FontWeight = 3,
                    Z_Index = 8,
                    IsItalic = false,
                    isBold = false,
                    Opacity = 1,
                    X = 150,
                    Y = 50,
                    ColorRGB = new List<byte>{255, 0, 0 },
                    BackgroundColorRBG = new List<byte>{200, 120, 120 },
                    Font = "Roboto",
                    IsCustomFont = true,
                    Text = @"{Name} is sold at {Price}{Currency}. IT has been the industry's standard dummy text ever since the 1500s,when an unknown printer took a of type and scrambled it to make a type specimen book"
                },
                new TextElement
                {
                    Height = 250,
                    Width = 400,
                    FontSize = 35,
                    FontWeight = 3,
                    Z_Index = 4,
                    IsItalic = false,
                    isUnderLine = true,
                    isBold = false,
                    Opacity = 1,
                    X = 150,
                    Y = 100,
                    ColorRGB = new List<byte>{255, 255, 255 },
                    BackgroundColorRBG = new List<byte>{0, 120, 120 },
                    Font = "Roboto",
                    IsCustomFont = true,
                    Text = "{Name} is the best",
                },
                new TextElement
                {
                    Height = 250,
                    Width = 400,
                    FontSize = 35,
                    FontWeight = 3,
                    Z_Index = 7,
                    IsItalic = false,
                    isUnderLine = true,
                    isBold = false,
                    Opacity = 1,
                    X = 150,
                    Y = 100,
                    ColorRGB = new List<byte>{255, 255, 255 },
                    BackgroundColorRBG = new List<byte>{0, 234, 255 },
                    Font = "Roboto",
                    IsCustomFont = true,
                    Text = "{Name} is the best",
                }
            },
                Height = 700,
                Width = 700
            };
            await GenerateImage(template, product);
        }

        public static async Task GenerateImage(Template template, Product product)
        {
            System.IO.Directory.CreateDirectory("output");

            var templateElements = new List<Element>();
            templateElements.AddRange(template.ImageElements);
            templateElements.AddRange(template.TextElements);
            templateElements.AddRange(template.ShapeElements);

            var orderdTemplateElements = templateElements.
                OrderBy(e => e.Z_Index);
            using (Image canvas = new Image<Rgba32>(template.Width, template.Height))
            {
                await LayerProductImage(product.ImageUrl, canvas, template);
                foreach (var element in orderdTemplateElements)
                {
                    if (element is ImageElement imageElement)
                    {
                        await LayerImage(canvas, imageElement);
                    }

                    if (element is TextElement textElement)
                    {
                        LayerText(textElement, product, canvas);
                    }
                }

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
            canvas.Mutate(c => c.Fill(Color.White)
                .DrawImage(backgroundImage, 1));
        }

        private static async Task LayerImage(Image canvas, ImageElement element)
        {
            var flipMode = SixLabors.ImageSharp.Processing.FlipMode.None;
            if (element.IsFlipped)
            {
                flipMode = (FlipMode)element.FlipMode;
            }
            var image = await DownloadImageFromUrl(element.ImageUrl);
            image.Mutate(x => x.Resize(element.Width, element.Height)
                .Opacity((float)(element.Opacity))
                .Rotate((float)element.Degree)
                .Flip(flipMode));

            canvas.Mutate(ctx => ctx.DrawImage(image, new Point(element.X, element.Y), 1)); // add Z index
        }

        private static void LayerText(TextElement element, Product product, Image canvas)
        {
            var productJObject = JObject.FromObject(product);

            var text = BuildDynamicText(productJObject, element.Text);

            var textBackground = new Image<Rgba32>(element.Width, element.Height);
            var font = GetFont(element);

            var textGraphicsOptions = new TextGraphicsOptions()
            {
                TextOptions = {
                        WrapTextWidth = element.Width,
                        HorizontalAlignment = HorizontalAlignment.Center },
                GraphicsOptions = { }
            };

            if (element.BackgroundColorRBG != null)
            {
                textBackground.Mutate(a => a.Fill(Color.FromRgb(element.BCR, element.BCG, element.BCB)));
            }
            textBackground.Mutate(a => a.DrawText(textGraphicsOptions, text, font, Color.FromRgb(element.CR, element.CG, element.CB), new PointF(0, 0)));

            canvas.Mutate(ctx => ctx.DrawImage(textBackground, new Point(element.X, element.Y), (float)element.Opacity));
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

            // TODO: item.FontFamily needs to be supported. (must be a valid FontFamily value)
            // TODO: handle custom fonts from user
            if (item.IsCustomFont)
            {
                // get the font from the DB, and apply it and return
                var fontCollection = LoadCustomFonts();

                if (fontCollection.TryFind(item.Font, out FontFamily fontFamily))
                {
                    var font = fontFamily.CreateFont(item.FontSize, fontStyle);

                    return font;
                }
                else
                {
                    return SystemFonts.CreateFont(item.Font, item.FontSize, fontStyle);
                }
            }
            else
            {
                return SystemFonts.CreateFont(item.Font, item.FontSize, fontStyle);
            }
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

        private static FontCollection LoadCustomFonts()
        {
            var collection = new FontCollection();
            collection.Install(Path.Combine("Resources", "Fonts", "Nunito.ttf"));
            collection.Install(Path.Combine("Resources", "Fonts", "Roboto.ttf"));
            return collection;
        }
    }
}
