using Gapotchenko.FX.AppModel;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Deployment;

sealed class ProductInformationService : IProductInformationService
{
    ProductInformationService()
    {
    }

    [Export]
    public static IProductInformationService Default { get; } = new ProductInformationService();

    static readonly IAppInformation m_Information = AppInformation.For(typeof(ProductInformationService));

    public string Name => m_Information.ProductName ?? throw new InvalidOperationException();

    public string Copyright => m_Information.Copyright ?? throw new InvalidOperationException();

    public Version Version => m_Information.ProductVersion;

    public Version FormalVersion => new(Version.Major, Version.Minor);

    public string Command => "turbo-coco";
}
