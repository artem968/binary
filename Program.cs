using System;
using System.IO;
using System.Text;

class Program
{
    const string Version = "2.1.0";
    const string Developer = "Artem Turenko";

    static void Main(string[] args)
    {
        if (args.Length == 1 && (args[0].Equals("--version", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"binary version {Version}");
            Console.WriteLine($"Developer: {Developer}");
            return;
        }
        if (args.Length == 1 && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase)))
        {
            PrintUsage();
            return;
        }
        if (args.Length < 2)
        {
            PrintUsage();
            return;
        }

        bool force = false;
        bool isInteractive = false;
        string? interactiveFlag = null;
        string? inputText = null;
        int fileArgIndex = 1;
        string? inputFile = null;
        string? outputDir = null;

        // Check for interactive mode first
        if (args[0].Equals("--i", StringComparison.OrdinalIgnoreCase) || 
            args[0].Equals("--insert", StringComparison.OrdinalIgnoreCase))
        {
            isInteractive = true;
            interactiveFlag = "encode";
            if (args.Length > 1)
            {
                inputText = args[1];
            }
        }
        else if (args[0].Equals("--ib", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("--insert-binary", StringComparison.OrdinalIgnoreCase))
        {
            isInteractive = true;
            interactiveFlag = "decode";
            if (args.Length > 1)
            {
                inputText = args[1];
            }
        }
        else
        {
            string flag = args[0].ToLower();
            if (!flag.Equals("--encode", StringComparison.OrdinalIgnoreCase) && 
                !flag.Equals("--decode", StringComparison.OrdinalIgnoreCase))
            {
                PrintUsage();
                return;
            }

            // Check for --f or --force after --decode
            if (flag == "--decode" && args.Length > 2 && 
                (args[2].Equals("--f", StringComparison.OrdinalIgnoreCase) || 
                 args[2].Equals("--force", StringComparison.OrdinalIgnoreCase)))
            {
                force = true;
                fileArgIndex = 1;
                inputFile = args[1];
                outputDir = args.Length > 3 ? args[3] : Path.GetDirectoryName(Path.GetFullPath(inputFile)) ?? ".";
            }
            else
            {
                inputFile = args[fileArgIndex];
                outputDir = args.Length > fileArgIndex + 1 ? args[fileArgIndex + 1] : 
                           Path.GetDirectoryName(Path.GetFullPath(inputFile)) ?? ".";
            }
        }

        try
        {
            if (isInteractive)
            {
                if (interactiveFlag == "encode")
                {
                    if (string.IsNullOrEmpty(inputText))
                    {
                        Console.WriteLine("Error: No text provided for encoding");
                        PrintUsage();
                        return;
                    }
                    EncodeString(inputText);
                }
                else if (interactiveFlag == "decode")
                {
                    if (string.IsNullOrEmpty(inputText))
                    {
                        Console.WriteLine("Error: No binary text provided for decoding");
                        PrintUsage();
                        return;
                    }
                    DecodeString(inputText, force);
                }
            }
            else
            {
                if (inputFile == null || outputDir == null)
                {
                    Console.WriteLine("Error: Input file or output directory is missing");
                    PrintUsage();
                    return;
                }

                string flag = args[0].ToLower();
                if (flag == "--encode")
                {
                    EncodeFile(inputFile, outputDir);
                }
                else if (flag == "--decode")
                {
                    DecodeFile(inputFile, outputDir, force);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine($"binary version {Version}");
        Console.WriteLine($"Developer: {Developer}");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  binary --encode <file> [output_dir]         Encode a file to <file>.binary");
        Console.WriteLine("  binary --decode <file> [output_dir]         Decode a .binary file to its original name");
        Console.WriteLine("  binary --decode <file> --force [output_dir] Force decode any file, output as <file>.decoded");
        Console.WriteLine("  binary --decode <file> --f [output_dir]     Same as --force, force decode any file");
        Console.WriteLine("  binary --encode --i \"text to encode\"        Encode text to binary");
        Console.WriteLine("  binary --encode --insert \"text to encode\"   Same as --i, encode text to binary");
        Console.WriteLine("  binary --decode --ib \"binary to decode\"     Decode binary to text");
        Console.WriteLine("  binary --decode --insert-binary \"binary\"    Same as --ib, decode binary to text");
        Console.WriteLine("  binary --version                            Show the current version");
        Console.WriteLine("  binary --help                               Show this help message");
    }

    static void EncodeFile(string inputFile, string outputDir)
    {
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Input file does not exist: {inputFile}");
            return;
        }
        string fileName = Path.GetFileName(inputFile) + ".binary";
        string outputPath = Path.Combine(outputDir, fileName);
        byte[] data = File.ReadAllBytes(inputFile);
        string base64Data = Convert.ToBase64String(data);
        File.WriteAllText(outputPath, base64Data);
        Console.WriteLine($"Encoded file saved to: {outputPath}");
    }

    static void DecodeFile(string inputFile, string outputDir, bool force = false)
    {
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Input file does not exist: {inputFile}");
            return;
        }
        if (!inputFile.EndsWith(".binary", StringComparison.OrdinalIgnoreCase) && !force)
        {
            Console.WriteLine("Input file must have a .binary extension for decoding. Use --f or --force to override.");
            return;
        }
        string fileName = inputFile.EndsWith(".binary", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(inputFile) : Path.GetFileName(inputFile) + ".decoded";
        string outputPath = Path.Combine(outputDir, fileName);
        string base64Data = File.ReadAllText(inputFile);
        byte[] data = Convert.FromBase64String(base64Data);
        File.WriteAllBytes(outputPath, data);
        Console.WriteLine($"Decoded file saved to: {outputPath}");
    }

    static void EncodeString(string input)
    {
        byte[] data = Encoding.UTF8.GetBytes(input);
        string binary = Convert.ToBase64String(data);
        Console.WriteLine(binary);
    }

    static void DecodeString(string binaryInput, bool force = false)
    {
        // Check if input contains non-binary characters
        if (!force && !IsValidBase64(binaryInput))
        {
            Console.WriteLine("Warning: Input contains non-binary characters. Use --force to decode anyway.");
            return;
        }

        try
        {
            byte[] data = Convert.FromBase64String(binaryInput);
            string decoded = Encoding.UTF8.GetString(data);
            Console.WriteLine(decoded);
        }
        catch (FormatException)
        {
            Console.WriteLine("Error: Invalid binary input format.");
        }
    }

    static bool IsValidBase64(string input)
    {
        try
        {
            Convert.FromBase64String(input);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
