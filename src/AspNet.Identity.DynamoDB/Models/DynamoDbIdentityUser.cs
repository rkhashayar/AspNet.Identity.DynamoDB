using Amazon.DynamoDBv2.DataModel;
using AspNet.Identity.DynamoDB.Constants;
using AspNet.Identity.DynamoDB.Converters;
using Microsoft.AspNetCore.Identity;

namespace AspNet.Identity.DynamoDB.Models;

[DynamoDBTable(TableNames.IdentityUsers)]
public class DynamoDbIdentityUser : DynamoDbIdentityUser<string>
{
    public DynamoDbIdentityUser()
    {
    }

    public DynamoDbIdentityUser(string email) : this()
    {
        Email = email;
        NormalizedEmail = email.ToUpper();
    }

    public DynamoDbIdentityUser(string email, string userName) : this()
    {
        UserName = userName ?? throw new ArgumentNullException(nameof(userName));
        NormalizedUserName = userName.ToUpper();
        Email = email;
        NormalizedEmail = email.ToUpper();
        Id = Guid.NewGuid().ToString();
        CreatedOn = DateTimeOffset.UtcNow;
    }
}

public class DynamoDbIdentityUser<TKey> where TKey : IEquatable<TKey>
{
    public DynamoDbIdentityUser() { }

    public DynamoDbIdentityUser(string userName) : this()
    {

        UserName = userName;
    }

    [DynamoDBHashKey]
    [PersonalData]
    public virtual TKey Id { get; set; } = default!;

    [ProtectedPersonalData]
    public virtual string? UserName { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("NormalizedUserName-DeletedOn-index")]
    public virtual string? NormalizedUserName { get; set; }

    [ProtectedPersonalData]
    public virtual string? Email { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("NormalizedEmail-DeletedOn-index")]

    public virtual string? NormalizedEmail { get; set; }

    [PersonalData]
    public virtual bool EmailConfirmed { get; set; }

    public virtual string? PasswordHash { get; set; }

    public virtual string? SecurityStamp { get; set; }

    public virtual string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    [ProtectedPersonalData]
    public virtual string? PhoneNumber { get; set; }

    [PersonalData]
    public virtual bool PhoneNumberConfirmed { get; set; }

    [PersonalData]
    public virtual bool TwoFactorEnabled { get; set; }

    public virtual DateTimeOffset? LockoutEnd { get; set; }

    public virtual bool LockoutEnabled { get; set; }

    public virtual int AccessFailedCount { get; set; }

    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset CreatedOn { get; set; }

    [DynamoDBGlobalSecondaryIndexRangeKey("NormalizedEmail-DeletedOn-index", "NormalizedUserName-DeletedOn-index", Converter = typeof(DateTimeOffsetConverter))]
    public DateTimeOffset DeletedOn { get; set; }
    public virtual void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public virtual void SetNormalizedUserName(string normalizedUserName) 
        => NormalizedUserName = normalizedUserName ?? throw new ArgumentNullException(nameof(normalizedUserName));
    public override string ToString() => UserName ?? string.Empty;
}