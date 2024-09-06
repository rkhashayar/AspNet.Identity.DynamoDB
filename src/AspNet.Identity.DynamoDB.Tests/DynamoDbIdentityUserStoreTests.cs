using AspNet.Identity.DynamoDB.Models;
using AspNet.Identity.DynamoDB.Stores;
using AspNet.Identity.DynamoDB.Tests.Helpers;
using FluentAssertions;

namespace AspNet.Identity.DynamoDB.Tests
{
    public class DynamoDbIdentityUserStoreTests
    {
        [Fact]
        public async Task Should_create_user()
        {
            var user = new DynamoDbIdentityUser("test@test.com", "test");
            using var dbProvider = DynamoDbServerHelpers.CreateDatabase();
            var userStore = new DynamoDbIdentityUserStore();
            await userStore.EnsureInitializedAsync(dbProvider.Client, dbProvider.Context);

            var result = await userStore.CreateAsync(user, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }
}