using Everlore.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Everlore.Infrastructure.Security;

public class DataProtectionEncryptionService(IDataProtectionProvider provider) : IEncryptionService
{
    private readonly IDataProtector _protector = provider.CreateProtector("DataSource.ConnectionString");

    public string Encrypt(string plainText) => _protector.Protect(plainText);

    public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
}
