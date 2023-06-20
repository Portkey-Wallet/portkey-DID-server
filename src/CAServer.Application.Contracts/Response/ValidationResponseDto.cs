using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Response;

[Serializable]
public class ValidationResponseDto : ResponseDto
{
    public IList<ValidationResult> ValidationErrors { get; set; }
}