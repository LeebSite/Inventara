using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.Json;
using Inventara.Application.AuditLogs;
using Inventara.Application.Auth;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Application.Security;
using Inventara.Application.Users;
using Inventara.Application.Validation;
using Inventara.Application.Vendors;
using Inventara.Domain.Common;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;
using Inventara.Infrastructure.Security;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Inventara.Tests;

public sealed class AuthAndMasterDataServiceTests
{
    [Fact]
    public async Task LoginAsync_ActiveUser_ReturnsTokenAndSafeProfile()
    {
        var user = CreateUser();
        var tokenIssuer = new FakeTokenIssuer();
        var service = CreateAuthService([user], tokenIssuer);

        var result = await service.LoginAsync(
            new LoginRequest { Email = user.Email, Password = "correct-password" },
            CancellationToken.None);

        Assert.Equal("test-token", result.AccessToken);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal("Admin", result.User.Role.ToString());
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(result));
        Assert.DoesNotContain(user.PasswordHash, JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsGenericCredentialsError()
    {
        var user = CreateUser();
        var service = CreateAuthService([user]);

        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.LoginAsync(
                new LoginRequest { Email = user.Email, Password = "wrong-password" },
                CancellationToken.None));

        Assert.Equal("Email or password is incorrect.", exception.Message);
        Assert.DoesNotContain("wrong-password", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_HashesPasswordAndDefaultsRole()
    {
        var repository = new FakeUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var auditLogs = new FakeAuditLogService();
        var service = CreateAuthService(repository, passwordHasher, auditLogs: auditLogs);

        var result = await service.RegisterAsync(
            Guid.NewGuid(),
            new RegisterRequest
            {
                FullName = "New User",
                Email = "NEW.USER@example.test",
                Password = "new-password",
                Department = "Operations",
                Phone = "123456"
            },
            CancellationToken.None);

        var storedUser = Assert.Single(repository.Items);
        Assert.Equal(Role.User, storedUser.Role);
        Assert.Equal("new.user@example.test", storedUser.Email);
        Assert.Equal("hashed:new-password", storedUser.PasswordHash);
        Assert.Equal(storedUser.Id, result.Id);
        var audit = Assert.Single(auditLogs.Entries);
        Assert.Equal("Create", audit.Action);
        Assert.DoesNotContain("PasswordHash", audit.NewValues);
        Assert.DoesNotContain("hashed:new-password", audit.NewValues);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ThrowsConflict()
    {
        var existingUser = CreateUser();
        var service = CreateAuthService([existingUser]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegisterAsync(
                Guid.NewGuid(),
                new RegisterRequest
                {
                    FullName = "Duplicate",
                    Email = existingUser.Email,
                    Password = "valid-password",
                    Department = "Operations",
                    Phone = "123456"
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task UserService_GetPage_SearchesAndPaginatesWithoutPasswordHash()
    {
        var users = new[]
        {
            CreateUser("Ada Lovelace", "ada@example.test"),
            CreateUser("Grace Hopper", "grace@example.test"),
            CreateUser("Deleted User", "deleted@example.test", isDeleted: true)
        };
        var service = CreateUserService(new FakeUserRepository(users));

        var result = await service.GetPageAsync(
            new ListQuery { Search = "example", Page = 1, PageSize = 1, SortBy = "email" },
            CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task UserService_UpdateWithPassword_HashesPasswordAndReturnsSafeResponse()
    {
        var user = CreateUser();
        var repository = new FakeUserRepository([user]);
        var auditLogs = new FakeAuditLogService();
        var service = CreateUserService(repository, auditLogs);

        var result = await service.UpdateAsync(
            user.Id,
            Guid.NewGuid(),
            new UpdateUserRequest
            {
                FullName = user.FullName,
                Email = user.Email,
                Password = "replacement-password",
                Role = Role.Staff,
                Department = user.Department,
                Phone = user.Phone,
                IsActive = true
            },
            CancellationToken.None);

        Assert.Equal("hashed:replacement-password", user.PasswordHash);
        Assert.Equal(Role.Staff, result.Role);
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(result));
        var audit = Assert.Single(auditLogs.Entries);
        Assert.DoesNotContain(user.PasswordHash, audit.NewValues);
    }

    [Fact]
    public async Task UserService_Delete_SoftDeletesAccount()
    {
        var user = CreateUser();
        var service = CreateUserService(new FakeUserRepository([user]));

        await service.DeleteAsync(user.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(user.IsDeleted);
    }

    [Fact]
    public async Task CreateVendorRequestValidator_InvalidEmail_FailsValidation()
    {
        var validator = new CreateVendorRequestValidator();

        var result = await validator.ValidateAsync(new CreateVendorRequest
        {
            Name = "Vendor",
            ContactPerson = "Contact",
            Email = "not-an-email",
            Phone = "123456",
            Address = "Address"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateVendorRequest.Email));
    }

    [Fact]
    public void JwtTokenIssuer_IssuedToken_HasEightHourLifetimeAndProfileClaims()
    {
        var signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var settings = JwtSettings.Create("Inventara.Tests", "Inventara.Tests", signingKey);
        var user = CreateUser();
        var result = new JwtTokenIssuer(settings).Issue(user);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(result.Value);

        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(user.Email, token.Claims.Single(claim => claim.Type == "email").Value);
        Assert.Equal(user.Role.ToString(), token.Claims.Single(claim => claim.Type == "role").Value);
        Assert.Equal(user.FullName, token.Claims.Single(claim => claim.Type == "name").Value);
        Assert.Equal(TimeSpan.FromHours(8), token.ValidTo - token.ValidFrom);
        Assert.Equal(
            new DateTimeOffset(token.ValidTo, TimeSpan.Zero).ToUnixTimeSeconds(),
            result.ExpiresAt.ToUnixTimeSeconds());
    }

    private static AuthService CreateAuthService(
        IEnumerable<User> users,
        FakeTokenIssuer? tokenIssuer = null) =>
        CreateAuthService(new FakeUserRepository(users), new FakePasswordHasher(), tokenIssuer);

    private static AuthService CreateAuthService(
        FakeUserRepository repository,
        FakePasswordHasher? passwordHasher = null,
        FakeTokenIssuer? tokenIssuer = null,
        FakeAuditLogService? auditLogs = null) =>
        new(
            repository,
            passwordHasher ?? new FakePasswordHasher(),
            tokenIssuer ?? new FakeTokenIssuer(),
            new LoginRequestValidator(),
            new RegisterRequestValidator(),
            auditLogs ?? new FakeAuditLogService());

    private static UserService CreateUserService(
        FakeUserRepository repository,
        FakeAuditLogService? auditLogs = null) =>
        new(
            repository,
            new FakePasswordHasher(),
            new ListQueryValidator(),
            new UpdateUserRequestValidator(),
            auditLogs ?? new FakeAuditLogService());

    private static User CreateUser(
        string fullName = "Admin User",
        string email = "admin@example.test",
        bool isDeleted = false) =>
        new()
        {
            FullName = fullName,
            Email = email,
            PasswordHash = "hashed:correct-password",
            Role = Role.Admin,
            Department = "IT",
            Phone = "123456",
            IsActive = true,
            IsDeleted = isDeleted
        };

    private sealed class FakeTokenIssuer : IJwtTokenIssuer
    {
        public IssuedToken Issue(User user) =>
            new("test-token", DateTimeOffset.UtcNow.AddHours(8));
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string passwordHash) =>
            passwordHash == Hash(password);
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<AuditLogEntry> Entries { get; } = [];

        public Task AddAsync(
            Guid? userId,
            string entityName,
            Guid entityId,
            string action,
            object? oldValues,
            object? newValues,
            CancellationToken cancellationToken)
        {
            Entries.Add(new AuditLogEntry(
                userId,
                entityName,
                entityId,
                action,
                oldValues is null ? null : JsonSerializer.Serialize(oldValues),
                newValues is null ? null : JsonSerializer.Serialize(newValues)));
            return Task.CompletedTask;
        }

        public Task<PagedResponse<AuditLogResponse>> GetPageAsync(
            AuditLogQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResponse<AuditLogResponse>([], 0, 0, query.Page, query.PageSize));

        public sealed record AuditLogEntry(
            Guid? UserId,
            string EntityName,
            Guid EntityId,
            string Action,
            string? OldValues,
            string? NewValues);
    }

    private sealed class FakeUserRepository(IEnumerable<User>? users = null)
        : FakeRepository<User>(users), IUserRepository
    {
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(user =>
                !user.IsDeleted && user.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> EmailExistsAsync(
            string normalizedEmail,
            Guid? exceptUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(user =>
                user.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)
                && (exceptUserId is null || user.Id != exceptUserId)));
    }

    private class FakeRepository<TEntity>(IEnumerable<TEntity>? entities = null)
        : IEntityRepository<TEntity>
        where TEntity : BaseEntity
    {
        public List<TEntity> Items { get; } = entities?.ToList() ?? [];

        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(entity => entity.Id == id && !entity.IsDeleted));

        public Task<bool> AnyIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(predicate.Compile()));

        public Task<PagedResult<TEntity>> GetPageAsync(
            Expression<Func<TEntity, bool>> predicate,
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken)
        {
            var property = typeof(TEntity).GetProperty(sortBy)!;
            var filtered = Items.Where(entity => !entity.IsDeleted && predicate.Compile()(entity));
            var ordered = descending
                ? filtered.OrderByDescending(entity => property.GetValue(entity))
                : filtered.OrderBy(entity => property.GetValue(entity));
            var totalItems = ordered.Count();
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return Task.FromResult(new PagedResult<TEntity>(items, totalItems, totalPages, page, pageSize));
        }

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(TEntity entity)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(1);
    }
}
