using AspNet.Identity.DynamoDB.Models;
using AspNet.Identity.DynamoDB.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspNet.Identity.DynamoDB.Extensions;

public static class DynamoDbServiceExtension
{
    public static void AddDynamoDbIdentity(this IServiceCollection services)
    {
        services.AddSingleton<IUserStore<DynamoDbIdentityUser>, DynamoDbIdentityUserStore>();
        services.AddSingleton<IUserEmailStore<DynamoDbIdentityUser>, DynamoDbIdentityUserStore>();
        services.AddSingleton<IPasswordHasher<DynamoDbIdentityUser>, PasswordHasher<DynamoDbIdentityUser>>();
        services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.AddSingleton<IdentityErrorDescriber>();
        services.AddSingleton<UserManager<DynamoDbIdentityUser>>();
        services.TryAddSingleton<IUserClaimsPrincipalFactory<DynamoDbIdentityUser>, UserClaimsPrincipalFactory<DynamoDbIdentityUser>>();
        services.AddScoped<SignInManager<DynamoDbIdentityUser>>();
        services.AddScoped<IUserConfirmation<DynamoDbIdentityUser>, DefaultUserConfirmation<DynamoDbIdentityUser>>();
    }
}