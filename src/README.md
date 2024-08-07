# Setup DynamoDB Locally 

following instructions to run dynamodb in a container
https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DynamoDBLocal.DownloadingAndRunning.html#docker
go to src folder and run `docker-compose up`

https://medium.com/@shahabaj.s.shaikh/local-development-with-dynamodb-setup-and-usage-guide-8d3a9adec626
aws dynamodb create-table --table-name sample --attribute-definitions AttributeName=Name,AttributeType=S AttributeName=city,AttributeType=S --key-schema AttributeName=Name,KeyType=HASH AttributeName=city,KeyType=RANGE --provisioned-throughput ReadCapacityUnits=1,WriteCapacityUnits=1 --table-class STANDARD --endpoint-url http://localhost:8000
aws dynamodb list-tables --endpoint-url http://localhost:8000