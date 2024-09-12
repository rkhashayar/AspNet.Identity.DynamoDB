using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNet.Identity.DynamoDB.Extensions;
using AspNet.Identity.DynamoDB.Models;
using AspNet.Identity.DynamoDB.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace WebApiSample;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        services.Configure<JwtSettings>(jwtSettings);
        services
            .AddAuthentication()
            .AddJwtBearer(
                x =>
                {
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });
        services.AddAuthorization();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        var dynamoDbSettings = configuration.GetSection("DynamoDbSettings");
        services.Configure<DynamoDbSettings>(dynamoDbSettings);
        services.AddDynamoDbIdentity();

        services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sample API", Version = "v1" }); });
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var dynamoDbSettings = app
            .ApplicationServices
            .GetService<IOptions<DynamoDbSettings>>();
        AmazonDynamoDBClient dynamoDbClient;
        if (env.IsDevelopment())
        {
            dynamoDbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
            {
                ServiceURL = dynamoDbSettings.Value.ServiceUrl
            });
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = string.Empty;
            });
        }
        else
            dynamoDbClient = new AmazonDynamoDBClient();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());

        var dynamoDbContext = new DynamoDBContext(dynamoDbClient);

        var userStore = app
                .ApplicationServices
                .GetService<IUserStore<DynamoDbIdentityUser>>()
            as DynamoDbIdentityUserStore;
        userStore
            .EnsureInitializedAsync(dynamoDbClient, dynamoDbContext, dynamoDbSettings.Value.UsersTableName)
            .Wait();
    }
}