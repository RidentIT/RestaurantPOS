using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Authentication.Commands.UpdateProfile;

/// <summary>
/// Updates the signed-in user's own display name and email. The username, role and module
/// grants are not touched here — those remain an administrator's call, made through the user
/// management endpoints.
/// </summary>
public sealed record UpdateProfileCommand(string FullName, string? Email) : IRequest<Result<UserDto>>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(User.FullNameMaxLength);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(User.EmailMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

internal sealed class UpdateProfileCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return Result.Failure<UserDto>(AuthErrors.InvalidCredentials);
        }

        var user = await db.Users
            .Include(u => u.ModulePermissions)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(AuthErrors.InvalidCredentials);
        }

        user.UpdateProfile(request.FullName, request.Email);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(user.ToDto());
    }
}
