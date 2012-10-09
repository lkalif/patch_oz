using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;
using System.IO;

namespace patch_oz
{
    public class CommandLine
    {
        [Option("u", "url", HelpText = "New login URL to use (http://127.0.0.1:8080/)")]
        public string NewURL = @"http://127.0.0.1:8080/";

        [ValueList(typeof(List<string>))]
        public IList<string> Files = null;

        public HelpText GetHeader()
        {
            HelpText header = new HelpText("Patch Oz: Change the hardcoded login URL of the Second Life viewer");
            header.AdditionalNewLineAfterOption = true;
            header.Copyright = new CopyrightInfo("Latif Khalifa", 2012);
            header.AddPreOptionsLine("https://bitbucket.org/lkalif/patch_oz");
            return header;
        }

        [HelpOption("h", "help", HelpText = "Display this help screen.")]
        public string GetUsage()
        {
            HelpText usage = GetHeader();
            usage.AddOptions(this);
            usage.AddPostOptionsLine("Usage: patch_oz [options] sl_executable");
            usage.AddPostOptionsLine("");
            usage.AddPostOptionsLine("Example: change login URL to OSGrid");
            usage.AddPostOptionsLine("patch_oz -u \"http://login.osgrid.org/\" SecondLife.exe");
            return usage.ToString();
        }
    }

    class Program
    {
        public static string originalString = "https://login.aditi.lindenlab.com/cgi-bin/login.cgi\0";
        public static byte[] originalBytes;
        public static byte[] newBytes;

        static void ExitWithError(string msg)
        {
            Console.WriteLine(msg);
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            // Init vars
            originalBytes = System.Text.UTF8Encoding.UTF8.GetBytes(originalString);

            // Read command line options
            var CommandLine = new CommandLine();
            CommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
            if (!parser.ParseArguments(args, CommandLine))
            {
                Environment.Exit(1);
            }

            if (CommandLine.Files == null || CommandLine.Files.Count == 0)
            {
                ExitWithError(CommandLine.GetUsage());
            }

            Console.WriteLine(CommandLine.GetHeader().ToString());
            Console.WriteLine();

            string newString = CommandLine.NewURL + "\0";
            newBytes = System.Text.UTF8Encoding.UTF8.GetBytes(newString);

            if (newBytes.Length > originalBytes.Length)
            {
                ExitWithError(string.Format("Cannot replace login URL with a string that is longer than original: {0}", originalString));
            }

            // Read the whole file into memory (RAM is cheap these days :P )
            string fileName = CommandLine.Files[0];
            byte[] file = null;

            bool readBackup = false;

            try
            {
                file = File.ReadAllBytes(fileName + ".orig");
                readBackup = true;
                Console.WriteLine(string.Format("Reading '{0}'. If you want to read '{1}' delete .orig file first.", fileName + ".orig", fileName));
            }
            catch {}

            if (!readBackup)
            {
                try
                {
                    file = File.ReadAllBytes(fileName);
                    File.WriteAllBytes(fileName + ".orig", file);
                    Console.WriteLine(string.Format("Saved original file to '{0}'", fileName + ".orig"));
                }
                catch
                {
                    ExitWithError(string.Format("Failed to read '{0}' into '{1}'", fileName, fileName + ".orig"));
                }
            }

            Console.WriteLine(string.Format("Read {0} bytes. Searching for '{1}'", file.Length, originalString));
            Console.WriteLine(string.Format("Replacing with '{0}'", newString));

            List<int> matches = new List<int>();

            for (int i = 0; i < file.Length - originalBytes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < originalBytes.Length; j++)
                {
                    if (file[i + j] != originalBytes[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    matches.Add(i);
                }
            }

            if (matches.Count == 0)
            {
                ExitWithError("Failed to find the search string.");
            }

            for (int i = 0; i < matches.Count; i++)
            {
                Console.WriteLine(string.Format("Match at 0x{0:x}", matches[i]));
                for (int j = 0; j < newBytes.Length; j++)
                {
                    file[j + matches[i]] = newBytes[j];
                }
                Console.WriteLine("Patched.");
            }

            try
            {
                File.WriteAllBytes(fileName, file);
                Console.WriteLine(string.Format("Saved changes to '{0}'.", fileName));
            }
            catch
            {
                ExitWithError(string.Format("Failed to save '{0}'.", fileName));
            }
        }
    }
}
