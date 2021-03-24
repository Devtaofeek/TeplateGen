using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing.Processing.Processors.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ConsoleApp1
{
    static class Program
    {
     static Product  product = new Product
        {
            Currency = "USD",
            Name = "Headphone",
            Price = 50,
            ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?ixid=MXwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHw%3D&ixlib=rb-1.2.1&auto=format&fit=crop&w=1920&q=1280"
        };

     private static Template template = new Template
     {
         ImageElements = new List<ImageElement>()
         {
             new ImageElement
             {
                 Y = 0, X = 0, Height = 100, Width = 100,Opacity = 0.5M,
                 ImageUrl = "https://thumbs.dreamstime.com/b/discount-stamp-vector-clip-art-33305813.jpg"
             }
         },
         TextElements = new List<TextElement>
         {
             new TextElement
             {
                 Height = 130, Width = 170, FontSize = 25, FontWeight = 3, Z_Index = 1, IsItalic = false, isBold = true,
                 Opacity = 1, X = 150, Y = 50, Color = "Whit", FontFamily = "Arial",BackgroundColor = "Transparent",
                 Text =
                     @"{Name} is sold at {Price}{Currency}. IT has been the industry's standard dummy text ever since the 1500s,when an unknown printer took a of type and scrambled it to make a type specimen book"
             },
             new TextElement
             {
                 Height = 200, Width = 400, FontSize = 35, FontWeight = 3, Z_Index = 1, IsItalic = true,
                 isUnderLine = true, isBold = true, Opacity = 1, X = 50, Y = 220, Color = "Red",
                 BackgroundColor = "Orange", FontFamily = "Calibri",
                 Text = "{Name} is the best",
             }
         },
         Height = 500,
         Width = 1000
     };
        private static HttpWebRequest webClient;
        static void Main(string[] args)
        {
            /*var jsonStringProduct = JsonConvert.SerializeObject(product);
            var width = 700;
            var height = 350;
            System.IO.Directory.CreateDirectory("output");
            using (Image img = new Image<Rgba32>(width, height))
            {
                Image smallImage = Image.Load(@"C:\Users\taofeeal\Downloads\Discount_logo.png");
                Image defaultimage = DownloadImageFromUrl("https://media.wired.com/photos/5f52a44bb555bc55dbcdf5a8/master/w_2560%2Cc_limit/Gear-Wireless-Bluetooth-1226031847.jpg");
               
                defaultimage.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                         Size = new Size(width,height), Mode = ResizeMode.Pad,Position = AnchorPositionMode.Center
                         
                    })
                    //.RotateFlip(RotateMode.Rotate180,FlipMode.Horizontal)
                    .Opacity((float)(1)));
                
                smallImage.Mutate(x=>x.Resize(100,100).Opacity((float)(0.5)));
                
                var text1Font = SystemFonts.CreateFont("Arial", 40, FontStyle.Italic);
                var text2Font = SystemFonts.CreateFont("Calibri", 50, FontStyle.Bold);
               
                string text1 = BuildDynamicText(jsonStringProduct,"Sony {Name}") ;
                string text2 = BuildDynamicText(jsonStringProduct,"{Price} USD");
                var textGraphicsOptions = new TextGraphicsOptions()
                {
                    TextOptions = {
                        WrapTextWidth = 30, 
                    },GraphicsOptions =
                    {
                        
                    }
                };
                
                var text2bg = new Image<Rgba32>(170, 130);
                var text1bg = new Image<Rgba32>(400, 200);
                text1bg.Mutate(ctx=>ctx.Fill(Color.Transparent).Rotate(RotateMode.Rotate270).DrawText(textGraphicsOptions, text1, text1Font, Color.White, new PointF(0, 0)).Rotate(77));
                text2bg.Mutate(ctx=>ctx.Fill(Color.Orange).DrawText(textGraphicsOptions, text2,text2Font,Color.Red, new PointF(10,0)));
                img.Mutate(ctx => ctx
                    .Fill(Color.Transparent)
                    .DrawImage(defaultimage, 1)
                    .DrawImage(smallImage, new Point(0, 0), 1)
                    .DrawImage(text1bg, new Point(150,50),1)
                    .DrawImage(text2bg,new Point(50,220), (float)0.5));
                img.Save("output/wordart.png");*/
           // }
           
           GenerateTemplate();
        }



        public static void GenerateTemplate()
        {
            System.IO.Directory.CreateDirectory("output");
            var backgroundImage = DownloadImageFromUrl(product.ImageUrl);
            using (Image canvas = new Image<Rgba32>(template.Width, template.Height))
            {
                backgroundImage.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(template.Width, template.Height), Mode = ResizeMode.Pad,Position = AnchorPositionMode.Center
                    })
                    .Opacity((float)(1)));
                
                if (template.ImageElements.Count > 0)
                {
                    foreach (var item in template.ImageElements)
                    {
                        var image = DownloadImageFromUrl(item.ImageUrl);
                        image.Mutate(x=>x.Resize(100,100).Opacity((float)(item.Opacity)));
                        canvas.Mutate(ctx=>ctx.DrawImage(image,new Point(item.X, item.Y),1));
                    }
                }
                
                
                if (template.TextElements.Count > 0)
                {
                    foreach (var item in template.TextElements)
                    {
                        var jsonStringProduct = JsonConvert.SerializeObject(product);
                        var textbg = new Image<Rgba32>(item.Width, item.Height);
                        var text = BuildDynamicText(jsonStringProduct,item.Text);
                        FontStyle fontStyle = item.IsItalic ? FontStyle.Italic : FontStyle.Regular;
                        fontStyle = item.isBold ? FontStyle.Bold :  FontStyle.Regular;
                        var textFont = SystemFonts.CreateFont("Arial", item.FontSize, fontStyle);
                        var textGraphicsOptions = new TextGraphicsOptions()
                        {
                            TextOptions = {
                                WrapTextWidth = 30, 
                            },GraphicsOptions =
                            {
                        
                            }
                        };
                        textbg.Mutate(a=>a.Fill(Color.Parse(item.BackgroundColor))
                            .DrawText(textGraphicsOptions,text,textFont,Color.FromRgb(),new PointF(0,0) ));
                        canvas.Mutate(ctx=>ctx.DrawImage(textbg, new Point(item.X,item.Y), (float)item.Opacity));
                    }
                }
                
                canvas.Save("output/wordart.png");
            }
        }
        
        
        
        private static string BuildDynamicText(string jsonStringProduct, string text)
        {
            var tokens = new Dictionary<string,string>();
            var product = JObject.Parse(jsonStringProduct);
            Regex reg = new Regex(@"{\w+}");
            foreach (Match match in reg.Matches(text))
            {
                var propertyName = match.Value.Substring(1, match.Value.Length-2);
                tokens.Add(match.Value, product.Value<string>(propertyName));
            }
            foreach (var token in tokens)
            {
                text = text.Replace(token.Key, tokens[token.Key]);
            }
            return text;
        }
        private static Image DownloadImageFromUrl(string item)
        {
            
            webClient = (HttpWebRequest)HttpWebRequest.Create(item);
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