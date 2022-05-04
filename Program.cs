using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Steganography
{
    class Program
    {
        static void Main(string[] args)
        {
            switch(args.Length > 0 ? args[0] : null)
            {
                case "embed":
                    if(args.Length != 4)
                    {
                        DisplayHelp();
                        return;
                    }

                    Steganography.EmbedInImage(args[1], args[2], args[3]);
                    break;
                case "extract":
                    if (args.Length != 3)
                    {
                        DisplayHelp();
                        return;
                    }

                    Steganography.ExtractFromImage(args[1], args[2]);
                    break;
                default:
                    DisplayHelp();
                    break;
            }
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Usages:");
            Console.WriteLine("  steganography embed input_png  input_payload  output_png");
            Console.WriteLine("  steganography extract input_png  output_file");
            Console.WriteLine("");
        }
    }
}