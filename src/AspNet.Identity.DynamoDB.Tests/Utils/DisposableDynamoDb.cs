using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace AspNet.Identity.DynamoDB.Tests.Utils;

internal class DisposableDynamoDb : IDisposable
{
    private bool _disposed;

    public DisposableDynamoDb()
    {
        var creds = new BasicAWSCredentials("test", "test");
        Client = new AmazonDynamoDBClient(creds, new AmazonDynamoDBConfig
        {
            ServiceURL = "http://localhost:8000"
        });
        Context = new DynamoDBContext(Client);
    }

    public IAmazonDynamoDB Client { get; }

    public IDynamoDBContext Context { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Client.Dispose();
        _disposed = true;
    }

}