using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;

namespace ConsoleTester
{
    class Program
    {
        struct Params
        {
            public int dbSize;
            public ulong itemCount;
            public int matchItems;
            public int iterations;
            public FileInfo parameters;
        }

        static int Main(string[] args)
        {
            RootCommand root = new(description: "APSILibrary tester")
            {
             new Option<int>(
                aliases: new string[] { "--dbSize", "-d" },
                description: "DB size",
                getDefaultValue: () => 65536),
            new Option<ulong>(
                aliases: new string[] { "--itemCount", "-i" },
                description: "Item count",
                getDefaultValue: () => 300),
            new Option<int>(
                aliases: new string[] { "--matchItems", "-m" },
                description: "Match items",
                getDefaultValue: () => 150),
            new Option<int>(
                aliases: new string[] { "--iterations", "-t" },
                description: "Number of iterations",
                getDefaultValue: () => 100),
            new Option<FileInfo>(
                aliases: new string[] {"--parameters", "-p"},
                description: "Parameters for the library",
                getDefaultValue: () => null) { Required = true }
            };

            Params parsedParams = new();

            ParseResult parseResult = root.Parse(args);
            if (parseResult.Errors.Count > 0)
            {
                Console.WriteLine("Argument errors:");
                foreach (ParseError error in parseResult.Errors)
                {
                    Console.WriteLine(error.Message);
                }
                return -1;
            }

            parsedParams.dbSize = parseResult.ValueForOption<int>("-d");
            parsedParams.itemCount = parseResult.ValueForOption<ulong>("-i");
            parsedParams.matchItems = parseResult.ValueForOption<int>("-m");
            parsedParams.iterations = parseResult.ValueForOption<int>("-t");
            parsedParams.parameters = parseResult.ValueForOption<FileInfo>("-p");

            if (null == parsedParams.parameters)
            {
                Console.WriteLine("Parameters were not provided.");
                return -1;
            }

            Console.WriteLine("Running test!");

            string jsonParams = File.ReadAllText(parsedParams.parameters.FullName);

            Tester.Test(jsonParams,
                        parsedParams.dbSize,
                        parsedParams.itemCount,
                        parsedParams.matchItems,
                        parsedParams.iterations);

            return 0;
        }

        static string GetParameters(string path)
        {
            Console.WriteLine($"Reading file: {path}");
            string parameters = File.ReadAllText(path);
            Console.WriteLine("Finished reading.");
            return parameters;
        }
    }
}
