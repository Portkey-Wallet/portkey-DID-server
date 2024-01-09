namespace SignatureServer.Command;

public interface ICommand
{

    string Name();
    
    void Run();

}