namespace CAServer.Commons;

public enum ResponseCode
{
    Succeed = 200,
    RequestError = 400,
    NoPermission = 401,
    SessionTimeout = 402,
    ServerError = 500,
    BusinessError = 501,
}