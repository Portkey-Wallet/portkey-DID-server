using Volo.Abp.AspNetCore.Mvc;

namespace CAServer.Signature;

public abstract class CAServerSignatureController : AbpControllerBase
{
    protected CAServerSignatureController()
    {
        LocalizationResource = typeof(CAServerSignatureResource);
    }
}