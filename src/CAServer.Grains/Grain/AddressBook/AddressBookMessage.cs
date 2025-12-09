namespace CAServer.Grains.Grain.AddressBook;

public class AddressBookMessage
{
    public const string NotExistMessage = "Contact not exist.";
    public const string ExistedMessage = "This name already exists";
    public const string HolderNullMessage = "CaHolder can not be null";
    public const string NameExistedCode = "40021";
    public const string AddressInvalidCode = "40022";
}