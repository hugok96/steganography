using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Steganography
{
    class Steganography
    {
        /// <summary>
        /// Embeds the contents of the inputPayload file into the inputImage, and outputs it to outputPath.
        /// The two least significant bits of each pixel are set changed to hold a single byte of info from the inputPayload.
        /// i.e. ARGB(0b00000010, 0b00000001, 0b00000011, 0b00000000) has the payload byte 0b01010110)
        /// </summary>
        /// <param name="inputImage">Path to image file to embed the payload in</param>
        /// <param name="inputPayload">Path to binary file to embed in the image</param>
        /// <param name="outputPath">Path to save the modified PNG to</param>
        public static void EmbedInImage(string inputImage, string inputPayload, string outputPath)
        {
            // Open input files
            using var inPngStream = Image.FromFile(inputImage);
            using var inPayloadStream = new FileStream(inputPayload, FileMode.Open, FileAccess.Read);
            using var inPayload = new BinaryReader(inPayloadStream);
            var inImage = new Bitmap(inPngStream);

            // Position tracker + total pixel count
            int pos = 0, endPos = inImage.Width * inImage.Height;

            // Encode the 4-byte long length of the payload
            for (; pos < 4; pos++)
                EmbedByteInPixel(ref inImage, (byte)(inPayload.BaseStream.Length >> pos * 8), pos);

            // Until we reach the end of the payload OR the end of the image/pixel-space, encode a byte
            while (inPayload.BaseStream.Position < inPayload.BaseStream.Length && pos < endPos)
                EmbedByteInPixel(ref inImage, inPayload.ReadByte(), pos++);

            // Output image to new file
            inImage.Save(outputPath, ImageFormat.Png);
            inImage.Dispose();
        }

        /// <summary>
        /// Attempts to extract a payload which has been embedded into a PNG image (inputImage), and output it to outputPath
        /// </summary>
        /// <param name="inputImage">Path to PNG image file that holds the embedded payload</param>
        /// <param name="outputPath">Path to output file to which the payload will be written</param>
        public static void ExtractFromImage(string inputImage, string outputPath)
        {
            // Open input and output files
            using var outFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var outStream = new BinaryWriter(outFile);
            using var inPngStream = new FileStream(inputImage, FileMode.Open, FileAccess.Read);
            using var inImage = new Bitmap(inPngStream);

            // Position tracker, total pixel count, payload length
            int pos = 0, endPos = inImage.Width * inImage.Height;
            long length = 0;

            // Read the payload length from the first 4 bytes in the image
            for (; pos < 4; pos++)
                length |= (long)ReadByteFromImage(inImage, pos) << (pos * 8);

            // Increase by four to take into account the length-bytes themselves
            length += 4;

            // Until we've reached either the end of the payload, or the end of the image/pixel-space, read a byte from the image and write it to output
            while (pos < endPos && pos < length)            
                outStream.Write(ReadByteFromImage(inImage, pos++));            
        }

        /// <summary>
        /// Reads a byte from the image at the given position
        /// </summary>
        /// <param name="image">The image from which to read</param>
        /// <param name="pos">The position from which to read (zero indexed, left to right, top to bottom)</param>
        /// <returns></returns>
        private static byte ReadByteFromImage(Bitmap image, int pos)
        {
            // Default value null
            byte b = 0;

            // Read pixel from given position (converting the position to coordinates first), and convert it to a byte array
            var pixel = image.GetPixel(pos % image.Width, (int)Math.Floor((float)pos / image.Width));
            byte[] bytes = new byte[4] { pixel.A, pixel.R, pixel.G, pixel.B };

            // For each bit
            for (int i = 0; i < 8; i++)
            {
                // Set mask to check the LSB or the 1st to LSB
                byte mask = (byte)(i < 4 ? 0b01 : 0b10);

                // Set bit value to 1 if the mask matches
                SetBitValue(ref b, i, (bytes[i % 4] & mask) == mask);
            }

            return b;
        }

        /// <summary>
        /// Embeds a byte to the image at a given position
        /// </summary>
        /// <param name="image"></param>
        /// <param name="inByte"></param>
        /// <param name="pos"></param>
        private static void EmbedByteInPixel(ref Bitmap image, byte inByte, int pos)
        {
            // Get coordinates
            int x= pos % image.Width, y= (int)Math.Floor((float)pos / image.Width);
            
            // Get pixel and convert it to a byte array
            var pixel = image.GetPixel(x, y);
            byte[] bytes = new byte[4] { pixel.A, pixel.R, pixel.G, pixel.B };

            // For each bit
            for (int i = 0; i < 8; i++)
            {
                // Set mask to check the specific bit (LE, RTL)
                byte mask = (byte)(0b1 << i);

                // Set the LSB (or 1st to LSB for the second set of 4 bits) to 1 if inByte matches the mask, 0 otherwise
                SetBitValue(ref bytes[i % 4], i < 4 ? 0 : 1, (inByte & mask) == mask);
            }

            // Write pixel to image
            image.SetPixel(x, y, Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]));
        }

        /// <summary>
        /// Set a specific bit's value of a byte by reference, based on a condition
        /// </summary>
        /// <param name="inByte">The byte that will be changed</param>
        /// <param name="position">Which position to change, zero-indexed, right-to-left (little endian?)</param>
        /// <param name="condition">The condition that sets the bit's value (true = 1, false = 0)</param>
        private static void SetBitValue(ref byte inByte, int position, bool condition)
        {
            // Calculate the correct mask, e.g. pos = 0, mask = 0b1; pos = 4, mask = 0b10000
            byte mask = (byte)(0b1 << position);

            // Set bit value, if condition == true then bitwise OR the mask, otherwise bitwise AND the inverted mask
            inByte = (byte)(condition ? inByte | mask : inByte & (byte)~mask);
        }
    }
}
