using CAServer.Orleans.TestBase;

namespace CAServer;

public abstract class CAServerApplicationTestBase :  CAServerTestBase<CAServerApplicationTestModule>
{

}

public class CAServerApplicationOrleansTestBase:CAServerOrleansTestBase<CAServerApplicationTestModule>
{
    
}
