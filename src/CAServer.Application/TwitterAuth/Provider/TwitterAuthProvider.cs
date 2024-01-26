using Volo.Abp.DependencyInjection;

namespace CAServer.TwitterAuth.Provider;

public interface ITwitterAuthProvider
{
}

public class TwitterAuthProvider : ITwitterAuthProvider, ISingletonDependency
{
}