namespace Gapotchenko.Turbo.CocoR.Deployment;

#nullable enable

interface IProductInformationService
{
    /// <summary>
    /// Gets the product name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the product copyright.
    /// </summary>
    string Copyright { get; }

    /// <summary>
    /// Gets the product version.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the formal product version.
    /// </summary>
    Version FormalVersion { get; }

    /// <summary>
    /// Gets the product command name.
    /// </summary>
    string Command { get; }
}
