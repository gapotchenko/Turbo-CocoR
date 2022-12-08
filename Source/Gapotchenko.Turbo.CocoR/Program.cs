using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Compilation;
using Gapotchenko.Turbo.CocoR.Deployment;
using Gapotchenko.Turbo.CocoR.Diagnostics;
using Gapotchenko.Turbo.CocoR.Framework.Diagnostics;
using Gapotchenko.Turbo.CocoR.Options;
using Gapotchenko.Turbo.CocoR.Scaffolding;
using System.Composition.Hosting;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.NET;

static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Run(args);
            return 0;
        }
        catch (ProgramExitException e)
        {
            return e.ExitCode;
        }
        catch (Exception e)
        {
            DiagnosticService.Default.Error(e.Message, e.TryGetCode());
            return 1;
        }
    }

    static void Run(IReadOnlyList<string> args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Console.OutputEncoding = Encoding.UTF8;

        ShowLogo();
        Execute(args);
    }

    static void Execute(IReadOnlyList<string> args)
    {
        if (HandleIntCall(args))
            return;

        var optionsService = new OptionsService(args);

        CompositionHost CreateContainer() =>
            new ContainerConfiguration()
            .WithExport(ProductInformationService.Default)
            .WithExport<IOptionsService>(optionsService)
            .WithAssembly(typeof(Program).Assembly)
            .CreateContainer();

        var command = optionsService.Command;
        if (command == "new")
        {
            Console.WriteLine();

            var commandArgs = optionsService.CommandArguments;
            if (commandArgs.Count < 2)
                throw new Exception($"Invalid command-line parameters for the \"{command}\" command.");

            using var container = CreateContainer();
            var scaffolder = container.GetExport<IScaffoldingService>();
            foreach (var itemName in commandArgs.Skip(1).Distinct())
            {
                var templateName = scaffolder.CreateItem(commandArgs[0], itemName);
                Console.WriteLine($"New \"{templateName}\" file created successfully.");
            }
        }
        else if (optionsService.HasSourceFile)
        {
            Console.WriteLine();

            using var container = CreateContainer();
            var compiler = container.GetExport<Compiler>();
            compiler.Compile();
        }
        else
        {
            ShowCopyright();
            optionsService.WriteUsage(Console.Out);
            throw new ProgramExitException(1);
        }
    }

    static void ShowLogo()
    {
        var productInformationService = ProductInformationService.Default;
        Console.Write(productInformationService.Name);
        Console.Write(' ');
        Console.WriteLine(productInformationService.FormalVersion);
    }

    static void ShowCopyright()
    {
        var productInformationService = ProductInformationService.Default;
        Console.WriteLine(productInformationService.Copyright);
        Console.WriteLine();
    }

    static bool HandleIntCall(IReadOnlyList<string> args)
    {
        if (!(args.Count >= 2 && args[0] == "--int-call"))
            return false;

        switch (args[1])
        {
            case "project":
                {
                    if (args.Count < 3)
                        throw new Exception("Verb not specified.");
                    switch (args[2])
                    {
                        case "compile-grammar":
                            if (args.Count < 4)
                                throw new Exception("Too few command-line parameters.");
                            CompileProject(args[3], new[] { "--property", "Mode", "Integrated" }.Concat(args.Skip(4)).ToArray());
                            break;

                        default:
                            throw new Exception("Invalid command of an internal call verb.");
                    }
                }
                break;

            default:
                throw new Exception("Invalid internal call.");
        }

        return true;
    }

    static void CompileProject(string grammarFilePath, IReadOnlyList<string> args)
    {
        var grammarFile = new FileInfo(grammarFilePath);

        if (!grammarFile.Exists)
            throw new Exception(string.Format("Grammar file \"{0}\" does not exist.", grammarFilePath)).Categorize("TCR0003");

        bool grammarIsEmpty = grammarFile.Length == 0;
        if (!grammarIsEmpty)
        {
            // Use a text reader because the file can have a BOM but still be empty.
            using var tr = grammarFile.OpenText();
            grammarIsEmpty = tr.Read() == -1;
        }

        if (grammarIsEmpty)
            Execute(new[] { "new", "grammar", grammarFilePath }.Concat(args).ToArray());

        // TODO
    }
}
