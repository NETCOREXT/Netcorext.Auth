using FluentValidation;

namespace Netcorext.Auth.Authorization.Services.Authorization.User.Queries;

public class GetUserFunctionValidator : AbstractValidator<GetUserFunction>
{
    public GetUserFunctionValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
