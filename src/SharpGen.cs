// Author: Ryan Cobb (@cobbr_io)
// Project: SharpGen (https://github.com/cobbr/SharpGen)
// License: BSD 3-Clause

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using System.ComponentModel.DataAnnotations;

using Confuser.Core;
using Confuser.Core.Project;
using YamlDotNet.Serialization;

namespace SharpGen
{
    class SharpGen
    {
        static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();
            app.HelpOption("-? | -h | --help");
            app.ThrowOnUnexpectedArgument = false;

            // Required arguments
            CommandOption OutputFileOption = app.Option(
                "-f | --file <OUTPUT_FILE>",
                "The output file to write to.",
                CommandOptionType.SingleValue
            ).IsRequired();

            // Compilation-related arguments
            CommandOption<Compiler.DotNetVersion> DotNetVersionOption = app.Option<Compiler.DotNetVersion>(
                "-d | --dotnet | --dotnet-framework <DOTNET_VERSION>",
                "The Dotnet Framework version to target (net35 or net40).",
                CommandOptionType.SingleValue
            );
            DotNetVersionOption.Validators.Add(new MustBeDotNetVersionValidator());
            CommandOption OutputKindOption = app.Option(
                "-o | --output-kind <OUTPUT_KIND>",
                "The OutputKind to use (dll or console).",
                CommandOptionType.SingleValue
            );
            OutputKindOption.Validators.Add(new MustBeOutputKindValidator());
            CommandOption PlatformOption = app.Option(
                "-p | --platform <PLATFORM>",
                "The Platform to use (AnyCpy, x86, or x64).",
                CommandOptionType.SingleValue
            );
            PlatformOption.Validators.Add(new MustBePlatformValidator());
            CommandOption NoOptimizationOption = app.Option(
                "-n | --no-optimization",
                "Don't use source code optimization.",
                CommandOptionType.NoValue
            );
            CommandOption AssemblyNameOption = app.Option(
                "-a | --assembly-name <ASSEMBLY_NAME>",
                "The name of the assembly to be generated.",
                CommandOptionType.SingleValue
            );
            AssemblyNameOption.Validators.Add(new MustBeIdentifierValidator());

            // Source-related arguments
            CommandOption SourceFileOption = app.Option(
                "-s | --source-file <SOURCE_FILE>",
                "The source code to compile.",
                CommandOptionType.SingleValue
            ).Accepts(v => v.ExistingFile());
            CommandOption ClassNameOption = app.Option(
                "-c | --class-name <CLASS_NAME>",
                "The name of the class to be generated.",
                CommandOptionType.SingleValue
            );
            ClassNameOption.Validators.Add(new MustBeIdentifierValidator());

            CommandOption ConfuseOption = app.Option(
                "--confuse <CONFUSEREX_PROJECT_FILE>",
                "The ConfuserEx ProjectFile configuration.",
                CommandOptionType.SingleValue
            ).Accepts(v => v.ExistingFile());

            app.OnExecute(() =>
            {
                Compiler.CompilationRequest request = new Compiler.CompilationRequest();
                // Compilation options
                if (!SetRequestDirectories(ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Unable to specify CompilationRequest directories");
                    app.ShowHelp();
                    return -1;
                }
                if (!SetRequestDotNetVersion(DotNetVersionOption, ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Invalid DotNetVersion specified.");
                    app.ShowHelp();
                    return -2;
                }
                if (!SetRequestOutputKind(OutputKindOption, OutputFileOption, ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Invalid OutputKind specified.");
                    app.ShowHelp();
                    return -3;
                }
                if (!SetRequestPlatform(PlatformOption, ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Invalid Platform specified.");
                    app.ShowHelp();
                    return -4;
                }
                if (!SetRequestOptimization(NoOptimizationOption, ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Invalid NoOptimization specified.");
                    app.ShowHelp();
                    return -5;
                }
                if (!SetRequestAssemblyName(AssemblyNameOption, ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Invalid AssemblyName specified.");
                    app.ShowHelp();
                    return -6;
                }
                if (!SetRequestReferences(ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Unable to set CompilationRequest references.");
                    app.ShowHelp();
                    return -7;
                }
                if (!SetRequestEmbeddedResources(ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Unable to set CompilationRequest resources.");
                    app.ShowHelp();
                    return -8;
                }

                // Source options
                if (!SetRequestSource(SourceFileOption, ClassNameOption, app.RemainingArguments, ref request))
                {
                    SharpGenConsole.PrintFormattedErrorLine("Unable to create source code for request.");
                    app.ShowHelp();
                    return -9;
                }

                // Compile
                SharpGenConsole.PrintFormattedProgressLine("Compiling source: ");
                SharpGenConsole.PrintInfoLine(request.Source);
                try
                {
                    byte[] compiled = Compiler.Compile(request);

                    // Write to file
                    string path = Path.Combine(Common.SharpGenOutputDirectory, OutputFileOption.Value());
                    File.WriteAllBytes(path, compiled);

                    if (ConfuseOption.HasValue())
                    {
                        ConfuserProject project = new ConfuserProject();
                        System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                        string ProjectFile = String.Format(
                            File.ReadAllText(ConfuseOption.Value()),
                            Common.SharpGenOutputDirectory,
                            Common.SharpGenOutputDirectory,
                            OutputFileOption.Value()
                        );
                        doc.Load(new StringReader(ProjectFile));
                        project.Load(doc);
                        project.ProbePaths.Add(Common.Net35Directory);
                        project.ProbePaths.Add(Common.Net40Directory);

                        SharpGenConsole.PrintFormattedProgressLine("Confusing assembly...");
                        ConfuserParameters parameters = new ConfuserParameters();
                        parameters.Project = project;
                        parameters.Logger = new ConfuserConsoleLogger();
                        Directory.SetCurrentDirectory(Common.SharpGenRefsDirectory);
                        ConfuserEngine.Run(parameters).Wait();
                    }

                    SharpGenConsole.PrintFormattedHighlightLine("Compiled assembly written to: " + path);
                }
                catch (CompilerException e)
                {
                    SharpGenConsole.PrintFormattedErrorLine(e.Message);
                    return -10;
                }
                catch (ConfuserException e)
                {
                    SharpGenConsole.PrintFormattedErrorLine("Confuser Exception: " + e.Message);
                    return -11;
                }
                return 0;
            });
            app.Execute(args);
        }

        private static bool SetRequestDirectories(ref Compiler.CompilationRequest request)
        {
            request.SourceDirectory = Common.SharpGenDirectory;
            request.ReferenceDirectory = Common.SharpGenReferencesDirectory;
            request.ResourceDirectory = Common.SharpGenResourcesDirectory;
            return true;
        }

        private static bool SetRequestDotNetVersion(CommandOption<Compiler.DotNetVersion> DotNetVersionOption, ref Compiler.CompilationRequest request)
        {
            request.TargetDotNetVersion = DotNetVersionOption.HasValue() ? DotNetVersionOption.ParsedValue : Compiler.DotNetVersion.Net35;
            return true;
        }

        private static bool SetRequestOutputKind(CommandOption OutputKindOption, CommandOption OutputFileOption, ref Compiler.CompilationRequest request)
        {
            if (OutputKindOption.HasValue())
            {
                if (OutputKindOption.Value().Contains("console", StringComparison.OrdinalIgnoreCase) || OutputKindOption.Value().Contains("exe", StringComparison.OrdinalIgnoreCase))
                {
                    request.OutputKind = OutputKind.ConsoleApplication;
                }
                else if (OutputKindOption.Value().Contains("dll", StringComparison.OrdinalIgnoreCase) || OutputKindOption.Value().Contains("dynamicallylinkedlibrary", StringComparison.OrdinalIgnoreCase))
                {
                    request.OutputKind = OutputKind.DynamicallyLinkedLibrary;
                }
                else
                {
                    return false;
                }
            }
            else if (OutputFileOption.HasValue())
            {
                if (OutputFileOption.Value().EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    request.OutputKind = OutputKind.ConsoleApplication;
                }
                else if (OutputFileOption.Value().EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    request.OutputKind = OutputKind.DynamicallyLinkedLibrary;
                }
            }
            return true;
        }

        private static bool SetRequestPlatform(CommandOption PlatformOption, ref Compiler.CompilationRequest request)
        {
            if (PlatformOption.HasValue())
            {
                if (PlatformOption.Value().Equals("x86", StringComparison.OrdinalIgnoreCase))
                {
                    request.Platform = Platform.X86;
                }
                else if (PlatformOption.Value().Equals("x64", StringComparison.OrdinalIgnoreCase))
                {
                    request.Platform = Platform.X64;
                }
                else if (PlatformOption.Value().Equals("AnyCpu", StringComparison.OrdinalIgnoreCase))
                {
                    request.Platform = Platform.AnyCpu;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                request.Platform = Platform.AnyCpu;
            }
            return true;
        }

        private static bool SetRequestOptimization(CommandOption NoOptimizationOption, ref Compiler.CompilationRequest request)
        {
            request.Optimize = !NoOptimizationOption.HasValue();
            return true;
        }

        private static bool SetRequestAssemblyName(CommandOption AssemblyNameOption, ref Compiler.CompilationRequest request)
        {
            if (AssemblyNameOption.HasValue()) { request.AssemblyName = AssemblyNameOption.Value(); }
            return true;
        }

        private static bool SetRequestReferences(ref Compiler.CompilationRequest request)
        {
            using (TextReader reader = File.OpenText(Common.SharpGenReferencesConfig))
            {
                var deserializer = new DeserializerBuilder().Build();
                request.References = deserializer.Deserialize<List<Compiler.Reference>>(reader).ToList();
            }
            return true;
        }

        private static bool SetRequestEmbeddedResources(ref Compiler.CompilationRequest request)
        {
            using (TextReader reader = File.OpenText(Common.SharpGenResourcesConfig))
            {
                var deserializer = new DeserializerBuilder().Build();
                request.EmbeddedResources = deserializer.Deserialize<List<Compiler.EmbeddedResource>>(reader);
            }
            return true;
        }

        private static bool SetRequestSource(CommandOption SourceFileOption, CommandOption ClassNameOption, List<string> RemainingArguments, ref Compiler.CompilationRequest request)
        {
            string className = RandomString();
            string returnType = "";
            string functionName = "";
            string code = "";
            if (ClassNameOption.HasValue())
            {
                className = ClassNameOption.Value();
            }
            if (SourceFileOption.HasValue())
            {
                code = File.ReadAllText(SourceFileOption.Value());
            }
            else
            {
                code = string.Join(" ", RemainingArguments);
            }

            if (request.OutputKind == OutputKind.ConsoleApplication)
            {
                returnType = "void";
                functionName = "Main";
                if (!code.Contains("return;"))
                {
                    code = code + "\r\n\t" + "return;";
                }
            }
            else if (request.OutputKind == OutputKind.DynamicallyLinkedLibrary)
            {
                returnType = "object";
                functionName = "Execute";
                if (!code.Contains("return;"))
                {
                    code = "return " + code;
                }
            }
            if (code.Contains(" class ") || code.Contains("\nclass "))
            {
                request.Source = code;
            }
            else
            {
                request.Source = String.Format(WrapperFunctionFormat, className, returnType, functionName, code);
            }
            return true;
        }

        private class MustBeOutputKindValidator : IOptionValidator
        {
            public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
            {
                if (!option.HasValue()) { return ValidationResult.Success; }
                string val = option.Value().ToLower();

                if (val != "console" && val != "consoleapp" && val != "consoleapplication" &&
                   val != "dll" && val != "dynamicallylinkedlibrary" && val != "exe")
                {
                    return new ValidationResult($"Invalid --{option.LongName} specified.");
                }

                return ValidationResult.Success;
            }
        }

        private class MustBeDotNetVersionValidator : IOptionValidator
        {
            public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
            {
                if (!option.HasValue()) { return ValidationResult.Success; }
                string val = option.Value();

                if (!val.Contains("35") && !val.Contains("40"))
                {
                    return new ValidationResult($"Invalid --{option.LongName} specified.");
                }

                return ValidationResult.Success;
            }
        }

        private class MustBeIdentifierValidator : IOptionValidator
        {
            private static Regex identifierRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9]*$");
            public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
            {
                if (!option.HasValue()) { return ValidationResult.Success; }
                string val = option.Value();
                if (!identifierRegex.IsMatch(val))
                {
                    return new ValidationResult($"Invalid --{option.LongName} specified.");
                }

                return ValidationResult.Success;
            }
        }

        private class MustBePlatformValidator : IOptionValidator
        {
            public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
            {
                if (!option.HasValue()) { return ValidationResult.Success; }
                string val = option.Value().ToLower();

                if (val != "x86" && val != "x64" && val != "anycpu")
                {
                    return new ValidationResult($"Invalid --{option.LongName} specified.");
                }

                return ValidationResult.Success;
            }
        }

        private static Random random = new Random();
        private static string RandomString()
        {
            const string alphachars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return alphachars[random.Next(alphachars.Length)] + new string(Enumerable.Repeat(chars, random.Next(10, 30)).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static string WrapperFunctionFormat =
@"using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Security.Principal;
using System.Collections.Generic;
using SharpSploit.Credentials;
using SharpSploit.Enumeration;
using SharpSploit.Execution;
using SharpSploit.LateralMovement;
using SharpSploit.Generic;
using SharpSploit.Misc;

public static class {0}
{{
    static {1} {2}()
    {{
        {3}
    }}
}}";
    }
}
