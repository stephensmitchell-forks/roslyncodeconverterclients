﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoslynCodeConverter.Client.Proxies;
using RoslynCodeConverter.Client.Proxies.Models;

namespace FailCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                int okFiles = TraverseTree(options.Path);

                Console.WriteLine($"{okFiles} converted without converter exception");
                Console.WriteLine("Done traversing the directory");
                Console.Read();
            }
        }

        // Adapted from: https://msdn.microsoft.com/en-us/library/bb513869.aspx
        public static int TraverseTree(string root)
        {
            var client = new RoslynCodeConverter.Client.Proxies.RoslynCodeConverter();
            var dirs = new Stack<string>(20);
            int okFiles = 0;

            if (!System.IO.Directory.Exists(root))
            {
                Console.Write("Directory does not exist");
                return okFiles;
            }

            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;

                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                catch (Exception)
                {
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir, "*.cs");
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (string file in files)
                {
                    try
                    {
                        if (FileConvertedOk(file, client))
                            okFiles++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"INFO: Exception when trying to run conversion for {file}, {ex.Message}");
                        continue;
                    }
                }

                // Push the subdirectories onto the stack for traversal.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            return okFiles;
        }

        static bool FileConvertedOk(string file, RoslynCodeConverter.Client.Proxies.RoslynCodeConverter client)
        {
            string code = System.IO.File.ReadAllText(file);
            if (String.IsNullOrWhiteSpace(code)) return false;

            ConvertResponse result = client.Converter.Post(new ConvertRequest()
            {
                Code = code,
                RequestedConversion = "cs2vbnet"
            });

            if (true != result.ConversionOk)
            {
                Console.WriteLine("ERROR:" + file);
                Console.WriteLine(result.ErrorMessage);
                return false;
            }

            return true;
        }
    }
}
