using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Common.Security;
using RestaurantPOS.Application.Users.Common;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Users.Commands.CreateUser;

/// <summary>
/// Creates a staff account. The new user is always flagged to change their password at first
/// sign-in, which is how the owner's administrator account gets its own password.
/// </summary>
/// <param name="Modules">Ignored when <paramref name="Role"/> is Admin, who hold every module.</param>
public sealed record CreateUserCommand(
    string Username,
    string FullName,
    string? Email,
    string Password,
    UserRole Role,
    IReadOnlyCollection<AppModule>? Modules) : IRequest<Result<UserDto>>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
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

        RuleFor(x => x.Password).MustMeetPasswordPolicy();

        RuleFor(x => x.Role).IsInEnum().WithMessage("Select a valid role.");

        RuleFor(x => x.Modules).MustBeAssignableModules();
    }
}

internal sealed class CreateUserCommandHandler(IAppDbContext db, IPasswordHasher passwordHasher)
    : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        var exists = await db.Users.AnyAsync(u => u.Username == username, cancellationToken);
        if (exists)
        {
            return Result.Failure<UserDto>(UserErrors.UsernameTaken);
        }

        var user = User.Create(
            username,
            request.FullName,
            request.Email,
            passwordHasher.Hash(request.Password),
            request.Role,
            request.Modules,
            mustChangePassword: true);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(user.ToDto());
    }
}