using System.Security.Cryptography;
using System.Text;
using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Tenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record GenerateGatewayApiKeyCommand(
    Guid TenantId,
    string Name,
    DateTime? ExpiresAt = null) : IRequest<Result<GatewayApiKeyGeneratedDto>>;

public record GatewayApiKeyGeneratedDto(
    Guid Id,
    string Name,
    string ApiKey,
    string KeyPrefix,
    DateTime? ExpiresAt,
    DateTime CreatedAt);

public class GenerateGatewayApiKeyValidator : AbstractValidator<GenerateGatewayApiKeyCommand>
{
    public GenerateGatewayApiKeyValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class GenerateGatewayApiKeyHandler(ICatalogDbContext db)
    : IRequestHandler<GenerateGatewayApiKeyCommand, Result<GatewayApiKeyGeneratedDto>>
{
    public async Task<Result<GatewayApiKeyGeneratedDto>> Handle(
        GenerateGatewayApiKeyCommand request, CancellationToken cancellationToken)
    {
        var tenantExists = await db.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists)
            return Result.Failure<GatewayApiKeyGeneratedDto>(ResultErrorType.NotFound, "Tenant not found.");

        // Generate a secure random key
        var rawKey = $"evlr_gw_{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32))}";
        var keyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        var keyPrefix = rawKey[..16];

        var entity = new GatewayApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = request.Name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            ExpiresAt = request.ExpiresAt
        };

        db.GatewayApiKeys.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new GatewayApiKeyGeneratedDto(
            entity.Id, entity.Name, rawKey, keyPrefix, entity.ExpiresAt, entity.CreatedAt));
    }
}
