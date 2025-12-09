using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic.Core;
using CAServer.UserExtraInfo;

namespace CAServer.UserGuide.Dtos;

public class UserGuideFinishRequestDto : IValidatableObject
{
    public GuideType GuideType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var values = Enum.GetValues(typeof(GuideType)).ToDynamicList();
        if (values.Any(guideType => !values.Contains(GuideType)))
        {
            yield return new ValidationResult(
                "Invalid input.",
                new[] { "GuideType" }
            );
        }
    }
}