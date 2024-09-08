using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace AspNet.Identity.DynamoDB.Converters;

public class DateTimeOffsetConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value) => ((DateTimeOffset)value).ToString("o");

    public object FromEntry(DynamoDBEntry entry) => DateTimeOffset.ParseExact(entry.AsString(), "o", null);
}