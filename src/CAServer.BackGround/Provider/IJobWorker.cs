namespace CAServer.BackGround.Provider;

public interface IJobWorker
{
    public Task HandleAsync();
}