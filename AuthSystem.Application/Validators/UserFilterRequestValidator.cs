using AuthSystem.Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthSystem.Application.Validators
{
    public class UserFilterRequestValidator
        : AbstractValidator<UserFilterRequestDto>
    {
        public UserFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);

            RuleFor(x => x.SortBy)
                .Must(x =>
                    string.IsNullOrWhiteSpace(x) ||
                    new[] { "fullname", "email", "isactive", "createdon" }
                        .Contains(x.ToLower()))
                .WithMessage("SortBy must be FullName, Email, IsActive, or CreatedOn.");

            RuleFor(x => x.SortOrder)
                .Must(x =>
                    string.IsNullOrWhiteSpace(x) ||
                    x.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                    x.Equals("desc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("SortOrder must be 'asc' or 'desc'.");
        }
    }
}
