using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using AspNet.Identity.DynamoDB.Constants;
using AspNet.Identity.DynamoDB.Extensions;
using AspNet.Identity.DynamoDB.Helpers;
using AspNet.Identity.DynamoDB.Models;
using Microsoft.AspNetCore.Identity;

namespace AspNet.Identity.DynamoDB.Stores;

public class DynamoDbIdentityUserStore : 
    IUserPasswordStore<DynamoDbIdentityUser>,
    IUserEmailStore<DynamoDbIdentityUser>
{
    private IDynamoDBContext _context;

    public void Dispose()
    {
        _context.Dispose();
    }

    public Task<string> GetUserIdAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.UserName);
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
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (normalizedName == null)
        {
            throw new ArgumentNullException(nameof(normalizedName));
        }

        user.SetNormalizedUserName(normalizedName);

        return Task.FromResult(0);
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

    public async Task<DynamoDbIdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        if (normalizedUserName == null)
        {
            throw new ArgumentNullException(nameof(normalizedUserName));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var searchConfig = new QueryOperationConfig
        {
            IndexName = "NormalizedUserName-DeletedOn-index",
            KeyExpression = new Expression
            {
                ExpressionStatement = "NormalizedUserName = :name AND DeletedOn = :deletedOn",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":name", normalizedUserName },
                    { ":deletedOn", default(DateTimeOffset).ToString("o") }
                }
            },
            Limit = 1
        };
        var search = _context.FromQueryAsync<DynamoDbIdentityUser>(searchConfig);
        var users = await search.GetRemainingAsync(cancellationToken);
        return users?.FirstOrDefault();
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

    public Task SetPasswordHashAsync(DynamoDbIdentityUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.SetPasswordHash(passwordHash);

        return Task.FromResult(0);
    }

    public Task<string?> GetPasswordHashAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.PasswordHash != null);
    }

    public Task SetEmailAsync(DynamoDbIdentityUser user, string? email, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetEmailAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var email = user.Email;

        return Task.FromResult(email);
    }

    public Task<bool> GetEmailConfirmedAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetEmailConfirmedAsync(DynamoDbIdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<DynamoDbIdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        if (normalizedEmail == null)
        {
            throw new ArgumentNullException(nameof(normalizedEmail));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var search = _context.FromQueryAsync<DynamoDbIdentityUser>(new QueryOperationConfig
        {
            IndexName = "NormalizedEmail-DeletedOn-index",
            KeyExpression = new Expression
            {
                ExpressionStatement = "NormalizedEmail = :email AND DeletedOn = :deletedOn",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":email", normalizedEmail },
                    { ":deletedOn", default(DateTimeOffset).ToString("o") }
                }
            },
            Limit = 1
        });
        var users = await search.GetRemainingAsync(cancellationToken);
        return users?.FirstOrDefault();
    }

    public Task<string?> GetNormalizedEmailAsync(DynamoDbIdentityUser user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var normalizedEmail = user.NormalizedEmail;

        return Task.FromResult(normalizedEmail);
    }

    public Task SetNormalizedEmailAsync(DynamoDbIdentityUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        // This method can be called even if user doesn't have an e-mail.
        // Act cool in this case and gracefully handle.
        // More info: https://github.com/aspnet/Identity/issues/645

        if (normalizedEmail != null)
        {
            user.NormalizedEmail = normalizedEmail;
        }

        return Task.FromResult(0);
    }
}