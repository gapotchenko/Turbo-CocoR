using Gapotchenko.FX;
using Gapotchenko.FX.Linq;
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

        var optionsService = new OptionsService(args);

        if (!optionsService.NoLogo)
            ShowLogo();
        Execute(optionsService);
    }

    static void Execute(IOptionsService optionsService)
    {
        if (HandleIntCall(optionsService))
            return;

        CompositionHost CreateContainer() =>
            new ContainerConfiguration()
            .WithExport(ProductInformationService.Default)
            .WithExport(optionsService)
            .WithAssembly(typeof(Program).Assembly)
            .CreateContainer();

        var command = optionsService.Command;
        if (command == "new")
        {
            if (!optionsService.NoLogo)
                Console.WriteLine();

            var commandArgs = optionsService.CommandArguments;
            if (commandArgs.Count < 2)
                throw new Exception($"Invalid command-line parameters for the \"{command}\" command.");

            using var container = CreateContainer();
            var scaffolder = container.GetExport<IScaffoldingService>();
            foreach (var itemName in commandArgs.Skip(1).Distinct())
            {
                var templateName = scaffolder.CreateItem(commandArgs[0], itemName);
                if (!optionsService.Quiet)
                    Console.WriteLine($"New \"{templateName}\" file created successfully.");
            }
        }
        else if (optionsService.HasSourceFile)
        {
            if (!optionsService.NoLogo)
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

    static void Execute(IEnumerable<string> args) => Execute(new OptionsService(args.AsReadOnlyList()) { NoLogo = true });

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

    static bool HandleIntCall(IOptionsService optionsService)
    {
        if (!optionsService.IntCall)
            return false;

        var args = optionsService.CommandArguments;

        switch (optionsService.Command)
        {
            case "compile-project-grammar":
                if (args.Count < 1)
                    throw new Exception("Too few command-line parameters.");
                CompileProject(args[0], new[] { "--property", "Mode", "Integrated" }.Concat(args.Skip(1)).ToArray());
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
            Execute(new[] { "new", "grammar", grammarFilePath }.Concat(args));

        // TODO
    }
}
