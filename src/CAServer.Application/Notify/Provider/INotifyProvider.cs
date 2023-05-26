using System.Threading.Tasks;

namespace CAServer.Notify.Provider;

public interface INotifyProvider
{
    Task<T> GetDataFromCms<T>(string condition);
}