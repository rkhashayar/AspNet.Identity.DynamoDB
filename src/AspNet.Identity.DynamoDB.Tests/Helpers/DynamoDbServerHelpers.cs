using AspNet.Identity.DynamoDB.Tests.Utils;

namespace AspNet.Identity.DynamoDB.Tests.Helpers;

internal static class DynamoDbServerHelpers
{
    public static DisposableDynamoDb CreateDatabase() => new();

}