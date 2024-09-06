using System.Security.Claims;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNet.Identity.DynamoDB.Models;
using AspNet.Identity.DynamoDB.Stores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
        services
            .AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(
                x =>
                {
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });
        services.AddAuthorization();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.Configure<DynamoDbSettings>(dynamoDbSettings);
        services.AddSingleton<IUserStore<DynamoDbIdentityUser>, DynamoDbIdentityUserStore>();
        services.AddSingleton<IUserEmailStore<DynamoDbIdentityUser>, DynamoDbIdentityUserStore>();
        services.TryAddSingleton<IPasswordHasher<DynamoDbIdentityUser>, PasswordHasher<DynamoDbIdentityUser>>();
        services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.AddSingleton<IdentityErrorDescriber>();
        services.AddSingleton<UserManager<DynamoDbIdentityUser>>();
        services.AddScoped<IUserConfirmation<DynamoDbIdentityUser>, DefaultUserConfirmation<DynamoDbIdentityUser>>();
        services.TryAddSingleton<IUserClaimsPrincipalFactory<DynamoDbIdentityUser>, UserClaimsPrincipalFactory<DynamoDbIdentityUser>>();

        services.AddScoped<SignInManager<DynamoDbIdentityUser>>();
        services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" }); });
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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/",
                async context =>
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

    public class UserClaimsPrincipalFactory<TUser> : IUserClaimsPrincipalFactory<TUser>
        where TUser : class
    {
        public UserClaimsPrincipalFactory(
            UserManager<TUser> userManager,
            IOptions<IdentityOptions> optionsAccessor)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException(nameof(userManager));
            }

            if (optionsAccessor?.Value == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            UserManager = userManager;
            Options = optionsAccessor.Value;
        }

        public UserManager<TUser> UserManager { get; }

        public IdentityOptions Options { get; }

        public virtual async Task<ClaimsPrincipal> CreateAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var userId = await UserManager.GetUserIdAsync(user);
            var userName = await UserManager.GetUserNameAsync(user);
            var id = new ClaimsIdentity(Options.Tokens.AuthenticatorTokenProvider,
                Options.ClaimsIdentity.UserNameClaimType,
                Options.ClaimsIdentity.RoleClaimType);
            id.AddClaim(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId));
            id.AddClaim(new Claim(Options.ClaimsIdentity.UserNameClaimType, userName));
            if (UserManager.SupportsUserSecurityStamp)
            {
                id.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType,
                    await UserManager.GetSecurityStampAsync(user)));
            }

            if (UserManager.SupportsUserRole)
            {
                var roles = await UserManager.GetRolesAsync(user);
                foreach (var roleName in roles)
                {
                    id.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, roleName));
                }
            }

            if (UserManager.SupportsUserClaim)
            {
                id.AddClaims(await UserManager.GetClaimsAsync(user));
            }

            return new ClaimsPrincipal(id);
        }
    }
}