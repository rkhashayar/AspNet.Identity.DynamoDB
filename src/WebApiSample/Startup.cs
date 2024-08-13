using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNet.Identity.DynamoDB.Models;
using AspNet.Identity.DynamoDB.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using WebApiSample.Models;

namespace WebApiSample;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        var dynamoDbSettings = Configuration.GetSection("DynamoDbSettings");
        services.Configure<DynamoDbSettings>(dynamoDbSettings);
        services.AddSingleton<IUserStore<DynamoDbIdentityUser>, DynamoDbIdentityUserStore>();
        services.TryAddSingleton<IPasswordHasher<DynamoDbIdentityUser>, PasswordHasher<DynamoDbIdentityUser>>();
        services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.AddSingleton<IdentityErrorDescriber>();
        services.AddSingleton<UserManager<DynamoDbIdentityUser>, UserManager<DynamoDbIdentityUser>>();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
        });
        services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
            });
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });

        var dynamoDbSettings = app.ApplicationServices.GetService<IOptions<DynamoDbSettings>>();
        var client = env.IsDevelopment()
            ? new AmazonDynamoDBClient(new AmazonDynamoDBConfig
            {
                ServiceURL = dynamoDbSettings.Value.ServiceUrl
            })
            : new AmazonDynamoDBClient();
        var context = new DynamoDBContext(client);

        var userStore = app.ApplicationServices
                .GetService<IUserStore<DynamoDbIdentityUser>>()
            as DynamoDbIdentityUserStore;
        userStore.EnsureInitializedAsync(client, context, dynamoDbSettings.Value.UsersTableName).Wait();
    }
}