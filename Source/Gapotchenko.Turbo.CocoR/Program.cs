using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Compilation;
using Gapotchenko.Turbo.CocoR.Deployment;
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
            var output = Console.Error;
            output.Write("Error: ");
            output.WriteLine(e.Message);
            return 1;
        }
    }

    static void Run(IReadOnlyList<string> args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Console.OutputEncoding = Encoding.UTF8;

        var productInformationService = new ProductInformationService();

        Console.Write(productInformationService.Name);
        Console.Write(' ');
        Console.WriteLine(productInformationService.SignificantVersion);

        var optionsService = new OptionsService(args, productInformationService);

        CompositionHost CreateContainer() =>
            new ContainerConfiguration()
            .WithExport<IProductInformationService>(productInformationService)
            .WithExport<IOptionsService>(optionsService)
            .WithAssembly(typeof(Program).Assembly)
            .CreateContainer();

        var command = optionsService.Command;
        if (command == "new")
        {
            Console.WriteLine();

            var commandArgs = optionsService.CommandArguments;
            if (commandArgs.Count != 2)
                throw new Exception($"Invalid command-line parameters for the \"{command}\" command.");

            using var container = CreateContainer();
            var scaffolder = container.GetExport<IScaffoldingService>();
            var templateName = scaffolder.CreateItem(commandArgs[0], commandArgs[1]);

            Console.WriteLine($"New \"{templateName}\" file created successfully.");
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
            Console.WriteLine(productInformationService.Copyright);
            Console.WriteLine();

            optionsService.WriteUsage(Console.Out);
            throw new ProgramExitException(1);
        }
    }
}
