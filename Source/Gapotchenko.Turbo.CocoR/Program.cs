using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Compilation;
using Gapotchenko.Turbo.CocoR.Deployment;
using Gapotchenko.Turbo.CocoR.Options;
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
        Console.WriteLine(productInformationService.Version.ToString(2));

        var optionsService = new OptionsService(args, productInformationService);

        CompositionHost CreateContainer() =>
            new ContainerConfiguration()
            .WithExport<IProductInformationService>(productInformationService)
            .WithExport<IOptionsService>(optionsService)
            .WithAssembly(typeof(Program).Assembly)
            .CreateContainer();

        if (args.Count > 0 && optionsService.HasSourceFile)
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
