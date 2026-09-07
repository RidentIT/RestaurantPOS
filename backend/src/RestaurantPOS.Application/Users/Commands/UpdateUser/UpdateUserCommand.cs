using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Common;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Users.Commands.UpdateUser;

/// <summary>Updates a user's profile, username, role and module grants in one operation.</summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string Username,
    string FullName,
    string? Email,
    UserRole Role,
    IReadOnlyCollection<AppModule>? Modules) : IRequest<Result<UserDto>>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Must(User.IsValidUsername)
            .WithMessage(
                $"Username must be {User.UsernameMinLength}-{User.UsernameMaxLength} characters and may " +
                "contain only letters, digits, dots, hyphens and underscores.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(User.FullNameMaxLength);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(User.EmailMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Role).IsInEnum().WithMessage("Select a valid role.");

        RuleFor(x => x.Modules).MustBeAssignableModules();
    }
}

internal sealed class UpdateUserCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.ModulePermissions)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(UserErrors.NotFound(request.UserId));
        }

        var username = request.Username.Trim().ToLowerInvariant();

        if (username != user.Username)
        {
            var usernameTaken = await db.Users.AnyAsync(
                u => u.Id != user.Id && u.Username == username,
                cancellationToken);

            if (usernameTaken)
            {
                return Result.Failure<UserDto>(UserErrors.UsernameTaken);
            }

            user.ChangeUsername(username);
        }

        var roleIsChanging = user.Role != request.Role;

        if (roleIsChanging)
        {
            if (user.Id == currentUser.UserId)
            {
                return Result.Failure<UserDto>(UserErrors.CannotDemoteSelf);
            }

            if (user.IsSystemAdmin)
            {
                return Result.Failure<UserDto>(UserErrors.CannotModifySystemAdmin);
            }

            if (user.Role == UserRole.Admin && !await AnotherActiveAdminExistsAsync(user.Id, cancellationToken))
            {
                return Result.Failure<UserDto>(UserErrors.LastAdmin);
            }
        }

        user.UpdateProfile(request.FullName, request.Email);

        if (roleIsChanging)
        {
            user.ChangeRole(request.Role);
        }

        // ChangeRole clears grants, so modules are applied afterwards. The call is a no-op for
        // administrators, whose access comes from the role itself.
        user.ReplaceModuleGrants(request.Modules ?? []);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(user.ToDto());
    }

    private Task<bool> AnotherActiveAdminExistsAsync(Guid excludingUserId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(
            u => u.Id != excludingUserId && u.IsActive && u.Role == UserRole.Admin,
            cancellationToken);
}