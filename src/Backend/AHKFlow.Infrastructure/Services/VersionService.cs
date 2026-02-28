using System.Reflection;

namespace AHKFlow.Infrastructure.Services
{
    public class VersionService : IVersionService
    {
        public Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            var version = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "0.0.0-dev";
            
            return Task.FromResult(version);
        }
    }
}
