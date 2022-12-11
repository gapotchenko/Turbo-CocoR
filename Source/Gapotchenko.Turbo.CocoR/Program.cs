using Gapotchenko.FX;
using Gapotchenko.FX.Linq;
using Gapotchenko.Turbo.CocoR.Deployment;
using Gapotchenko.Turbo.CocoR.Diagnostics;
using Gapotchenko.Turbo.CocoR.Framework.Diagnostics;
using Gapotchenko.Turbo.CocoR.Options;
using Gapotchenko.Turbo.CocoR.Orchestration;
using System.Composition.Hosting;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace Gapotchenko.Turbo.CocoR;

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

    static CompositionHost CreateContainer(IOptionsService optionsService) =>
        new ContainerConfiguration()
        .WithExport(ProductInformationService.Default)
        .WithExport(optionsService)
        .WithAssembly(typeof(Program).Assembly)
        .CreateContainer();

    static void Execute(IOptionsService optionsService)
    {
        if (HandleIntCall(optionsService))
            return;

        var command = optionsService.Command;
        if (command == "new")
        {
            if (!optionsService.NoLogo)
                Console.WriteLine();

            var commandArgs = optionsService.CommandArguments;
            if (commandArgs.Count < 2)
                throw new Exception($"Invalid command-line parameters for the \"{command}\" command.");

            using var container = CreateContainer(optionsService);
            var orchestrationService = container.GetExport<IOrchestrationService>();
            orchestrationService.Scaffold(commandArgs[0], commandArgs.Skip(1).Distinct());
        }
        else if (optionsService.HasSourceFile)
        {
            if (!optionsService.NoLogo)
                Console.WriteLine();

            using var container = CreateContainer(optionsService);
            var orchestrationService = container.GetExport<IOrchestrationService>();
            orchestrationService.CompileGrammar();
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

    static bool HandleIntCall(IOptionsService optionsService)
    {
        if (!optionsService.IntCall)
            return false;

        var args = optionsService.CommandArguments;

        switch (optionsService.Command)
        {
            case "compile-project-grammar":
                {
                    if (args.Count < 1)
                        throw new Exception("Too few command-line parameters.");

                    using var container = CreateContainer(
                        new OptionsService(new[] { "-p", "Mode", "Integrated" }.Concat(args.Skip(1)).ToArray())
                        {
                            NoLogo = true
                        });
                    var orchestrationService = container.GetExport<IOrchestrationService>();
                    orchestrationService.CompileProjectGrammar(args[0]);
                }
                break;

            default:
                throw new Exception("Invalid internal call.");
        }

        return true;
    }
}
