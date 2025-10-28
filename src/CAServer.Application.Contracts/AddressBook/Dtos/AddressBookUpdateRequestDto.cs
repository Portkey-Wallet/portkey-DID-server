using System;

namespace CAServer.AddressBook.Dtos;

public class AddressBookUpdateRequestDto : AddressBookCreateRequestDto
{
    public Guid Id { get; set; }
}