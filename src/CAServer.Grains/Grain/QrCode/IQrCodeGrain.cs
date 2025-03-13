namespace CAServer.Grains.Grain.QrCode;

public interface IQrCodeGrain : IGrainWithStringKey
{
    Task<bool> AddIfAbsent();
}