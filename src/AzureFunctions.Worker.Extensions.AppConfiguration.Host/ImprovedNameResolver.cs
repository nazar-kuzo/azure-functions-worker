using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs;

/// <summary>
/// Improves existing name resolver with ability to resolve nested settings
/// </summary>
internal sealed class ImprovedNameResolver : DefaultNameResolver
{
    private readonly IConfiguration configuration;

    public ImprovedNameResolver(IConfiguration configuration)
        : base(configuration)
    {
        this.configuration = configuration;
    }

    public override string Resolve(string name)
    {
        return this.configuration.GetConnectionStringOrSetting(name) ?? base.Resolve(name);
    }
}
