using GeometryDashAPI.Data;
using GeometryDashAPI.Levels;
using GeometryDashAPI.Levels.GameObjects.Default;
using GeometryDashAPI.Levels.GameObjects.Triggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace ArtToGeometryDash
{
    public class Program
    {
        private static string levelName;
        private static int levelRevision;
        private static string fileArt;
        private static int artWight, artHeight;
        private static Bitmap art;
        private static byte[] filterColor;
        private static bool useAlphaChannel;
        private static byte alphaChannel;
        private static int colorStart;
        private static float scale;
        private static int EditorLayer;
        private static int blockStart;
        private static double userDistance;
        private static bool addTrigger;
        private static float coordX, coordY;
        private static LocalLevels localLevels;
        private static Dictionary<int, int> palette = new Dictionary<int, int>();
        private static Level level;
        private static bool filtering;
        private static string mode;
        private static short HSVChannel;

        const string consoleLine = "________________";

        private static int BytesToInt(byte r, byte g, byte b)
        {
            return r << 16 | g << 8 | b;
        }

        private static int? FindSimilar(byte r, byte g, byte b)
        {
            foreach (var element in palette)
            {
                if (element.Key < 1 && element.Key > 999)
                    continue;
                byte cr = level.Colors[(short)element.Value].Red;
                byte cg = level.Colors[(short)element.Value].Green;
                byte cb = level.Colors[(short)element.Value].Blue;

                double distance = Math.Sqrt(Math.Pow(r - cr, 2) + Math.Pow(g - cg, 2) + Math.Pow(b - cb, 2));
                if (distance < userDistance)
                    return element.Key;
            }
            return null;
        }

        private static bool FindSimilar(byte r, byte g, byte b, byte cr, byte cg, byte cb)
        {
            double distance = Math.Sqrt(Math.Pow(r - cr, 2) + Math.Pow(g - cg, 2) + Math.Pow(b - cb, 2));
            if (distance < userDistance)
                return true;
            return false;
        }

        private static Bitmap OpenResizeArt(string path, int wigth, int heigth)
        {
            Bitmap img = new Bitmap(path);
            Bitmap res = new Bitmap(wigth, heigth);
            var drawImage = Graphics.FromImage(res);

            drawImage.InterpolationMode = InterpolationMode.Bilinear;
            drawImage.DrawImage(img, 0, 0, artWight, artHeight);

            return res;
        }

        private static string RGB2HSVString(byte ri, byte gi, byte bi)
        {
            int h;
            float s, v;
            float max, min;
            
            float r = (float)ri / 255;
            float g = (float)gi / 255;
            float b = (float)bi / 255;

            max = (float)Math.Max(Math.Max(ri, gi), bi) / 255;
            min = (float)Math.Min(Math.Min(ri, gi), bi) / 255;

            if (max == r && g >= b)
                h = (int)(60 * (g - b) / (max - min));
            else if (max == r && g < b)
                h = (int)(60 * (g - b) / (max - min) + 360);
            else if (max == g)
                h = (int)(60 * (b - r) / (max - min) + 120);
            else
                h = (int)(60 * (r - g) / (max - min) + 240);

            if (h > 180)
                h -= 360;

            if (max == 0)
                s = 0;
            else
                s = 1 - min / max;

            v = max;

            return string.Format("{0}a{1}a{2}a0a0", h, Math.Round(s, 2), Math.Round(v, 2)).Replace(",", ".");
        }

        static void Main(string[] args)
        {
            Console.Title = "Art To Geometry Dash";
            Welcome();

            InputSettings();

            Console.Write("How to add image, HSV or RGB (HSV use only 1 color channel)\nWrite h or r\n> ");
            mode = Console.ReadLine().ToLower();
            if (mode == "h")
                InputHSVSettings();
            else
                InputRGBSettings();
            Console.Clear();

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            Console.WriteLine("Loading image ...");
            art = OpenResizeArt(fileArt, artWight, artWight);

            Console.WriteLine("Loading level ...");
            localLevels = new LocalLevels();
            if (levelRevision == -1)
                level = new Level(localLevels.GetLevel(levelName));
            else
                level = new Level(localLevels.GetLevel(levelName, levelRevision));
            blockStart = level.CountBlock;

            Console.WriteLine("Creating art...");
            if (mode == "h")
                CreateHSVart();
            else
                CreateRGBart();

            Console.WriteLine("Save...");
            localLevels.GetLevel(levelName).LevelString = level.ToString();
            localLevels.Save();

            sw.Stop();

            Console.Clear();
            if (mode == "h")
                Console.WriteLine($"Completed!\nUsed blocks: {level.CountBlock - blockStart}\nElapsed time: {sw.ElapsedTicks} ticks ({sw.ElapsedMilliseconds} milliseconds)");
            else
                Console.WriteLine($"Completed!\nUsed colors: {palette.Count}\nUsed blocks: {level.CountBlock - blockStart}\nElapsed time: {sw.ElapsedTicks} ticks ({sw.ElapsedMilliseconds} milliseconds)");
            Console.WriteLine("Press any button to continue.");
            Console.ReadKey();
        }

        private static void Welcome()
        {
            Console.WriteLine("Art To Geometry Dash.");
            Console.WriteLine("The program for the transfer of art and other images in Geometry Dash.");
            Console.WriteLine("Version: 2.0");
            Console.WriteLine($"{consoleLine}\n\nAuthors: Folleach and Relayx");
            Console.WriteLine($"{consoleLine}\n\nFor its work, the program uses the library GeometryDashAPI\nhttps://github.com/Folleach/GeometryDashAPI");
            Console.WriteLine($"{consoleLine}\n\nHSV and Resize mode by Nodus Lorden");
            Console.WriteLine($"{consoleLine}\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("You must close all instances of the game before moving the image!");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press any button to continue.");
            Console.ReadKey();
            Console.Clear();
        }

        private static void InputSettings()
        {
            Console.Write("Image path.\nExample: C:\\images\\my.png\n> ");
            fileArt = Console.ReadLine().Replace("\"", "");
            Console.Clear();

            Console.Write("Art Width and height.\nExample: 32 64\n> ");
            string[] artsize = Console.ReadLine().Split(' ');
            artWight = int.Parse(artsize[0]);
            artHeight = int.Parse(artsize[1]);
            Console.Clear();

            Console.Write("The size of the pixel in art.\n1: 1 pixel = 1/4 block\nExample: 0,8\n> ");
            scale = float.Parse(Console.ReadLine().Replace(",", "."), CultureInfo.InvariantCulture);
            Console.Clear();

            Console.Write("Alpha color.\nExample 1: 255 255 255 (Cut close to white color)\nExample 2: A 245 (Cuts all pixels with little aplha.)\nExample 3: N (for skip) \n> ");
            string[] al = Console.ReadLine().Split(' ');
            // Eng a and Rus а
            if (al.Length > 1)
            {
                if (al[0].ToLower() == "a" || al[0].ToLower() == "а")
                {
                    useAlphaChannel = true;
                    alphaChannel = byte.Parse(al[1]);
                }
                else
                {
                    filterColor = new byte[3];
                    filterColor[0] = byte.Parse(al[0]);
                    filterColor[1] = byte.Parse(al[1]);
                    filterColor[2] = byte.Parse(al[2]);

                    Console.Write("Color similarity ratio.\n0 - Exact color match\nThe higher the value, the less color will be used in the level.\nRecommended value: 10\n> ");
                    userDistance = double.Parse(Console.ReadLine());
                }
                filtering = true;
            }
            else
                filtering = false;
            Console.Clear();

            Console.Write("Layer for art.\n> ");
            EditorLayer = int.Parse(Console.ReadLine());
            Console.Clear();

            Console.Write("Image position in level.\nExample for position in cells: 10 5\nExample for position in coordinates: C 600 300\nUsing position far from 0 when adding a large image, otherwise the level may not open.\n> ");
            string[] coords = Console.ReadLine().Split(' ');
            if (coords[0].ToLower() == "c" || coords[0].ToLower() == "с")
            {
                coordX = float.Parse(coords[1]);
                coordY = float.Parse(coords[2]);
            }
            else
            {
                coordX = float.Parse(coords[0]) * 60 - 15;
                coordY = float.Parse(coords[1]) * 60;
            }
            Console.Clear();

            Console.Write("The name of the level in which the art will be placed.\nRecomended use empty level.\nPossible level damage!\n> ");
            levelName = Console.ReadLine();
            Console.Write("Level revision. Void or string if not revision.\n> ");
            string rev = Console.ReadLine();
            if (int.TryParse(rev, out int num))
                levelRevision = int.Parse(rev);
            else
                levelRevision = -1;
            Console.Clear();
        }

        private static void InputRGBSettings()
        {   
            if (!filtering)
            {
                Console.Write("Color similarity ratio.\n0 - Exact color match\nThe higher the value, the less color will be used in the level.\nRecommended value: 10\n> ");
                userDistance = double.Parse(Console.ReadLine());
                Console.Clear();
            }
            
            Console.Write("What ID to start creating the palette\n> ");
            colorStart = int.Parse(Console.ReadLine());
            Console.Clear();

            Console.Write("Add a color changing trigger?\nWrite Y or N\n> ");
            string addtrgstr = Console.ReadLine();
            addTrigger = addtrgstr.ToLower() == "yes" || addtrgstr.ToLower() == "y" ? true : false;
            Console.Clear();
        }

        private static void InputHSVSettings()
        {
            Console.Write("ID for base HSV color\n> ");
            HSVChannel = short.Parse(Console.ReadLine());
            Console.Clear();
        }

        private static void CreateRGBart()
        {
            int currentColorID = colorStart;

            for (int x = 0; x < art.Width; x += 1)
            {
                for (int y = 0; y < art.Height; y += 1)
                {
                    System.Drawing.Color color = art.GetPixel(x, y);
                    int colorInt = BytesToInt(color.R, color.G, color.B);
                    if (filtering)
                    {
                        if (useAlphaChannel)
                        {
                            if (color.A < alphaChannel)
                                continue;
                        }
                        else
                        {
                            if (FindSimilar(color.R, color.G, color.B, filterColor[0], filterColor[1], filterColor[2]))
                                continue;
                        }
                    }
                    if (!palette.ContainsKey(colorInt))
                    {
                        int? sim = FindSimilar(color.R, color.G, color.B);
                        if (sim == null)
                        {
                            level.AddColor(new GeometryDashAPI.Levels.Color((short)currentColorID, color.R, color.G, color.B));
                            palette.Add(colorInt, currentColorID);
                            if (addTrigger)
                            {
                                level.AddBlock(new PulseTrigger()
                                {
                                    TargetID = currentColorID,
                                    Red = color.R,
                                    Green = color.G,
                                    Blue = color.B,
                                    Hold = 10f,
                                    PositionX = +coordX,
                                    PositionY = 0 + coordY,
                                    EditorL = (short)EditorLayer
                                });
                            }
                            currentColorID++;
                        }
                        else
                        {
                            colorInt = (int)sim;
                        }
                    }
                    DetailBlock block = new DetailBlock(917);
                    block.WithoutLoaded.Add("21", palette[colorInt].ToString());
                    block.Scale = scale;
                    block.EditorL = (short)EditorLayer;
                    block.PositionX = (x * 7.5f * scale) + coordX;
                    block.PositionY = ((art.Height - y) * 7.5f * scale) + coordY;
                    level.AddBlock(block);
                }
            }
        }

        private static void CreateHSVart(){
            level.AddColor(new GeometryDashAPI.Levels.Color(HSVChannel, 255, 0, 0));

            for (int x = 0; x < art.Width; x += 1)
            {
                for (int y = 0; y < art.Height; y += 1)
                {
                    System.Drawing.Color color = art.GetPixel(x, y);
                    if (filtering){
                        if (useAlphaChannel)
                        {
                            if (color.A < alphaChannel)
                                continue;
                        }
                        else
                        {
                            if (FindSimilar(color.R, color.G, color.B, filterColor[0], filterColor[1], filterColor[2]))
                                continue;
                        }
                    }                    

                    DetailBlock block = new DetailBlock(917);
                    block.WithoutLoaded.Add("21", HSVChannel.ToString());
                    block.Scale = scale;
                    block.EditorL = (short)EditorLayer;
                    block.PositionX = (x * 7.5f * scale) + coordX;
                    block.PositionY = ((art.Height - y) * 7.5f * scale) + coordY;
                    block.WithoutLoaded.Add("41", "1");
                    block.WithoutLoaded.Add("43", RGB2HSVString(color.R, color.G, color.B));
                    level.AddBlock(block);
                }
            }
        }
    }
}
