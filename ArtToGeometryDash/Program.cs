using GeometryDashAPI.Data;
using GeometryDashAPI.Levels;
using GeometryDashAPI.Levels.GameObjects.Default;
using GeometryDashAPI.Levels.GameObjects.Triggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace ArtToGeometryDash
{
    class Program
    {
        static string levelName;
        static string fileArt;
        static int pixelSize;
        static Bitmap art;
        static byte[] alpha;
        static bool useAplhaChannel;
        static byte alphaChannel;
        static int colorStart;
        static float scale;
        static int EditorLayer;
        static int blockStart;
        static double userDistance;
        static bool addTrigger;
        static float coordX, coordY;
        static LocalLevels localLevels;
        static Dictionary<int, int> palette = new Dictionary<int, int>();
        static Level level;

        static int BytesToInt(byte r, byte g, byte b)
        {
            return r << 16 | g << 8 | b;
        }
        
        static int? FindSimilar(byte r, byte g, byte b)
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

        static bool FindSimilar(byte r, byte g, byte b, byte cr, byte cg, byte cb)
        {
            double distance = Math.Sqrt(Math.Pow(r - cr, 2) + Math.Pow(g - cg, 2) + Math.Pow(b - cb, 2));
            if (distance < userDistance)
                return true;
            return false;
        }

        static void Main(string[] args)
        {
            Console.Title = "Art To Geometry Dash";
            Welcome();
            Console.Write("Image path.\nExample: C:\\images\\my.png\n> ");
            fileArt = Console.ReadLine().Replace("\"", "");
            Console.Clear();
            Console.Write("Width and height pixel (pixels). \n> ");
            pixelSize = int.Parse(Console.ReadLine());
            Console.Clear();
            Console.Write("Alpha color.\nExample 1: 255 255 255 (Cut close to white color)\nExample 2: A 245 (Cuts all pixels with little aplha.)\n> ");
            string[] al = Console.ReadLine().Split(' ');
            if (al[0] == "A" || al[0] == "А")
            {
                useAplhaChannel = true;
                alphaChannel = byte.Parse(al[1]);
            }
            else
            {
                alpha = new byte[3];
                alpha[0] = byte.Parse(al[0]);
                alpha[1] = byte.Parse(al[1]);
                alpha[2] = byte.Parse(al[2]);
            }
            Console.Clear();
            Console.Write("Color similarity ratio.\n0 - Exact color match\nThe higher the value, the less color will be used in the level.\nRecommended value: 10\n> ");
            userDistance = double.Parse(Console.ReadLine());
            Console.Clear();
            Console.Write("What ID to start creating the palette\n> ");
            colorStart = int.Parse(Console.ReadLine());
            Console.Clear();
            Console.Write("The size of the art in the level.\n1: 1 pixel = 1/4 block\nExample: 0,8\n> ");
            scale = float.Parse(Console.ReadLine());
            Console.Clear();
            Console.Write("Image coordinates in level.\n1 block = 30, 2 block = 60\nTemplate: X Y\nExample: 2000 0\nUsing coordinates far from 0 when adding a large image, otherwise the level may not open.\n> ");
            string[] coords = Console.ReadLine().Split(' ');
            coordX = float.Parse(coords[0]);
            coordY = float.Parse(coords[1]);
            Console.Clear();
            Console.Write("Layer for art.\n> ");
            EditorLayer = int.Parse(Console.ReadLine());
            Console.Clear();
            Console.Write("Add a color changing trigger?\n1 - Yes\n0 - No\n> ");
            string addtrgstr = Console.ReadLine();
            addTrigger = addtrgstr.ToLower() == "yes" || addtrgstr == "1" ? true : false;
            Console.Clear();
            Console.Write("The name of the level in which the art will be placed.\nRecomended use empty level.\nPossible level damage!\n> ");
            levelName = Console.ReadLine();
            Console.Clear();

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            Console.WriteLine("Loading level ...");
            localLevels = new LocalLevels();
            level = new Level(localLevels.GetLevelByName(levelName), null, new List<int>());
            
            Console.WriteLine("Loading image ...");
            art = new Bitmap(fileArt);

            blockStart = level.CountBlock;

            Console.WriteLine("Creating art...");
            int currentColorID = colorStart;
            int startFor = 0;
            if (pixelSize >= 2)
                startFor = 1;
            else if (pixelSize >= 4)
                startFor = 2;
            else if (pixelSize >= 8)
                startFor = 4;
            for (int x = startFor; x < art.Width; x += pixelSize)
            {
                for (int y = startFor; y < art.Height; y += pixelSize)
                {
                    System.Drawing.Color color = art.GetPixel(x, y);
                    int colorInt = BytesToInt(color.R, color.G, color.B);
                    if (useAplhaChannel)
                    {
                        if (color.A < alphaChannel)
                            continue;
                    }
                    else
                    {
                        if (FindSimilar(color.R, color.G, color.B, alpha[0], alpha[1], alpha[2]))
                            continue;
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
                                    PositionX = -15 + coordX,
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
                    block.ColorDetail = (short)palette[colorInt];
                    block.Scale = scale;
                    block.EditorL = (short)EditorLayer;
                    block.PositionX = (x / pixelSize * 7.5f * scale) + coordX;
                    block.PositionY = ((art.Height - y) / pixelSize * 7.5f * scale) + coordY;
                    level.AddBlock(block);
                }
            }

            Console.WriteLine("Save...");
            localLevels.GetLevelByName(levelName).LevelString = level.ToString();
            localLevels.Save();

            sw.Stop();

            Console.Clear();
            Console.WriteLine($"Completed!\nUsed colors: {palette.Count}\nUsed blocks: {level.CountBlock - blockStart}\nElapsed time: {sw.ElapsedTicks} ticks ({sw.ElapsedMilliseconds} milliseconds)");
            Console.WriteLine("Press any button to continue.");
            Console.ReadKey();
        }

        const string consoleLine = "________________";
        private static void Welcome()
        {
            Console.WriteLine("Art To Geometry Dash.");
            Console.WriteLine("The program for the transfer of art and other images in Geometry Dash.");
            Console.WriteLine("Version: 1.1");
            Console.WriteLine($"{consoleLine}\n\nAuthor:\nFolleach - Creator of the program.\nRelayx - Creation Assistant.");
            Console.WriteLine($"{consoleLine}\n\nFor its work, the program uses the library GeometryDashAPI\nhttps://github.com/Folleach/GeometryDashAPI");
            Console.WriteLine($"{consoleLine}\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("You must close all instances of the game before moving the image!");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press any button to continue.");
            Console.ReadKey();
            Console.Clear();
        }
    }
}
