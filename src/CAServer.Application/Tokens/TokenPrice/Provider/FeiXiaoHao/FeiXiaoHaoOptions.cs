namespace CAServer.Tokens.TokenPrice.Provider.FeiXiaoHao;

public class FeiXiaoHaoOptions
{
    public string BaseUrl { get; set; } = "https://fxhapi.feixiaohao.com/public/v1/ticker";
    public int PageSize { get; set; } = 100;
    public int MaxPageNo { get; set; } = 5;
    public int Timeout { get; set; } = 10000;
    public int Priority { get; set; } = 1;
    public bool IsAvailable { get; set; } = true;
}