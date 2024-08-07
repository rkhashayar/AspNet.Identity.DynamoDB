using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using AspNet.Identity.DynamoDB.Constants;
using AspNet.Identity.DynamoDB.Extensions;
using AspNet.Identity.DynamoDB.Helpers;
using AspNet.Identity.DynamoDB.Models;
using Microsoft.AspNetCore.Identity;

namespace AspNet.Identity.DynamoDB.Stores;

public class DynamoDbIdentityUserStore : IUserStore<DynamoDbIdentityUser>
{
    private IDynamoDBContext _context;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task<string> GetUserIdAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetUserNameAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetUserNameAsync(DynamoDbIdentityUser user, string? userName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetNormalizedUserNameAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetNormalizedUserNameAsync(DynamoDbIdentityUser user, string? normalizedName,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<IdentityResult> CreateAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        cancellationToken.ThrowIfCancellationRequested();

        await _context.SaveAsync(user, cancellationToken);

        return IdentityResult.Success;
    }

    public Task<IdentityResult> UpdateAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> DeleteAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<DynamoDbIdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<DynamoDbIdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task EnsureInitializedAsync(
        IAmazonDynamoDB client,
        IDynamoDBContext context,
        string userTableName = TableNames.IdentityUsers)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));

        _context = context ?? throw new ArgumentNullException(nameof(context));

        var hasDifferentTableName = userTableName != TableNames.IdentityUsers;
        if (hasDifferentTableName)
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(userTableName, TableNames.IdentityUsers));

        return EnsureInitializedImplAsync(client, userTableName);
    }

    private async Task EnsureInitializedImplAsync(
        IAmazonDynamoDB client,
        string userTableName)
    {
        var defaultProvisionThroughput = new ProvisionedThroughput
        {
            ReadCapacityUnits = 1,
            WriteCapacityUnits = 1
        };
        var globalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new()
            {
                IndexName = "NormalizedUserName-DeletedOn-index",
                KeySchema =
                [
                    new KeySchemaElement("NormalizedUserName", KeyType.HASH),
                    new KeySchemaElement("DeletedOn", KeyType.RANGE)
                ],
                ProvisionedThroughput = defaultProvisionThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL
                }
            },
            new()
            {
                IndexName = "NormalizedEmail-DeletedOn-index",
                KeySchema =
                [
                    new KeySchemaElement("NormalizedEmail", KeyType.HASH),
                    new KeySchemaElement("DeletedOn", KeyType.RANGE)
                ],
                ProvisionedThroughput = defaultProvisionThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL
                }
            }
        };

        var tableNames = await client.ListAllTablesAsync();

        if (!tableNames.Contains(userTableName))
        {
            await CreateTableAsync(client, userTableName, defaultProvisionThroughput, globalSecondaryIndexes);
            return;
        }

        var response = await client.DescribeTableAsync(new DescribeTableRequest { TableName = userTableName });
        var table = response.Table;

        var indexesToAdd =
            globalSecondaryIndexes.Where(
                g => !table.GlobalSecondaryIndexes.Exists(gd => gd.IndexName.Equals(g.IndexName)));
        var indexUpdates = indexesToAdd.Select(index => new GlobalSecondaryIndexUpdate
        {
            Create = new CreateGlobalSecondaryIndexAction
            {
                IndexName = index.IndexName,
                KeySchema = index.KeySchema,
                ProvisionedThroughput = index.ProvisionedThroughput,
                Projection = index.Projection
            }
        }).ToList();

        if (indexUpdates.Count > 0) await UpdateTableAsync(client, userTableName, indexUpdates);
    }

    private static async Task CreateTableAsync(
        IAmazonDynamoDB client, 
        string userTableName,
        ProvisionedThroughput provisionedThroughput, 
        List<GlobalSecondaryIndex> globalSecondaryIndexes)
    {
        var response = await client.CreateTableAsync(new CreateTableRequest
        {
            TableName = userTableName,
            ProvisionedThroughput = provisionedThroughput,
            KeySchema =
            [
                new()
                {
                    AttributeName = "Id",
                    KeyType = KeyType.HASH
                },

                new()
                {
                    AttributeName = "DeletedOn",
                    KeyType = KeyType.RANGE
                }
            ],
            AttributeDefinitions =
            [
                new()
                {
                    AttributeName = "Id",
                    AttributeType = ScalarAttributeType.S
                },

                new()
                {
                    AttributeName = "DeletedOn",
                    AttributeType = ScalarAttributeType.S
                },

                new()
                {
                    AttributeName = "NormalizedUserName",
                    AttributeType = ScalarAttributeType.S
                },

                new()
                {
                    AttributeName = "NormalizedEmail",
                    AttributeType = ScalarAttributeType.S
                }
            ],
            GlobalSecondaryIndexes = globalSecondaryIndexes
        });

        if (response.HttpStatusCode != HttpStatusCode.OK) throw new Exception($"Couldn't create table {userTableName}");

        await DynamoDbHelpers.WaitForActiveTableAsync(client, userTableName);
    }

    private async Task UpdateTableAsync(
        IAmazonDynamoDB client, 
        string userTableName,
        List<GlobalSecondaryIndexUpdate> indexUpdates)
    {
        await client.UpdateTableAsync(new UpdateTableRequest
        {
            TableName = userTableName,
            GlobalSecondaryIndexUpdates = indexUpdates
        });

        await DynamoDbHelpers.WaitForActiveTableAsync(client, userTableName);
    }
}