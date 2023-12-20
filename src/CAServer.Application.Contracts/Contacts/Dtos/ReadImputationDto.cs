using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Contacts;

public class ReadImputationDto
{
    [Required] public Guid ContactId { get; set; }
}