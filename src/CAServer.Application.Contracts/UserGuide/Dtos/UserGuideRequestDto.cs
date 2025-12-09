using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic.Core;
using CAServer.UserExtraInfo;

namespace CAServer.UserGuide.Dtos;

public class UserGuideRequestDto : IValidatableObject
{
    public List<int> GuideTypes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var values = Enum.GetValues(typeof(GuideType));
        var guideTypes = values.Cast<int>().ToList();
        foreach (var guideType in GuideTypes.Where(guideType => !guideTypes.Contains(guideType)))
        {
            yield return new ValidationResult(
                "Invalid input.",
                new[] { "GuideTypes" }
            );
        }
    }
}