using FluentValidation;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserFunctionMixRoleValidator : AbstractValidator<GetUserFunctionMixRole>
{
    public GetUserFunctionMixRoleValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
