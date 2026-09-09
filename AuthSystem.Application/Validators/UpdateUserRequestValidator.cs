using AuthSystem.Application.DTOs.Users;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthSystem.Application.Validators
{
    public class UpdateUserRequestValidator
    : AbstractValidator<UpdateUserRequestDto>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Please provide a valid email address.");

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage("Role is required.");
        }
    }
}
