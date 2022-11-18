using Gapotchenko.FX.AppModel;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Deployment;

sealed class ProductInformationService : IProductInformationService
{
    static readonly IAppInformation m_Information = AppInformation.For(typeof(ProductInformationService));

    public string Name => m_Information.ProductName ?? string.Empty;

    public string Copyright => m_Information.Copyright ?? string.Empty;

    public Version Version => m_Information.ProductVersion;

    public string Command => "turbo-coco";
}
