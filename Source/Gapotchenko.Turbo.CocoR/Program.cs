using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Compilation;
using Gapotchenko.Turbo.CocoR.Deployment;
using Gapotchenko.Turbo.CocoR.Options;
using System.Composition.Hosting;

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
        var productInformationService = new ProductInformationService();

        Console.Write(productInformationService.Name);
        Console.Write(' ');
        Console.WriteLine(productInformationService.Version.ToString(3));
        Console.WriteLine();

        var optionsService = new OptionsService(args, productInformationService);

        CompositionHost CreateContainer() =>
            new ContainerConfiguration()
            .WithExport<IProductInformationService>(productInformationService)
            .WithExport<IOptionsService>(optionsService)
            .WithAssembly(typeof(Program).Assembly)
            .CreateContainer();

        if (args.Count > 0 && optionsService.HasSourceFileName)
        {
            using var container = CreateContainer();
            var compiler = container.GetExport<Compiler>();
            compiler.Compile();
        }
        else
        {
            optionsService.WriteUsage(Console.Out);
            throw new ProgramExitException(1);
        }
    }
}
