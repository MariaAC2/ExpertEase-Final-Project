using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Configurations;
using ExpertEase.Infrastructure.Database;
using ExpertEase.Infrastructure.Firestore.FirestoreRepository;
using ExpertEase.Infrastructure.Middlewares;
using ExpertEase.Infrastructure.Realtime;
using ExpertEase.Infrastructure.Repositories;
using ExpertEase.Infrastructure.Services;
using ExpertEase.Infrastructure.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using ReviewService = ExpertEase.Infrastructure.Services.ReviewService;
using StripeConfiguration = Stripe.StripeConfiguration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var firebaseFilePath = configuration["Firebase:PrivateKey"];

    if (string.IsNullOrEmpty(firebaseFilePath))
    {
        throw new InvalidOperationException("Firebase Admin SDK file path is missing.");
    }

    if (!File.Exists(firebaseFilePath))
    {
        throw new FileNotFoundException($"Firebase Admin SDK file not found at: {firebaseFilePath}");
    }

    var credential = GoogleCredential.FromFile(firebaseFilePath);
    var firestoreBuilder = new FirestoreClientBuilder
    {
        Credential = credential
    };

    var client = firestoreBuilder.Build();
    return FirestoreDb.Create("uniproject-38b1d", client);
});

builder.Services.AddHttpClient<UserService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "ExpertEase/1.0");
});
builder.Services.AddDbContext<WebAppDatabaseContext>(o => 
    o.UseNpgsql(builder.Configuration.GetConnectionString("PostgresDb")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
});
builder.Services.Configure<MailConfiguration>(builder.Configuration.GetSection(nameof(MailConfiguration)));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection(nameof(StripeConfiguration)));
builder.Services.Configure<ProtectionFeeSettings>(builder.Configuration.GetSection(nameof(ProtectionFeeSettings)));
builder.Services.Configure<GoogleOAuthConfiguration>(builder.Configuration.GetSection("GoogleOAuth"));
builder.Services.AddScoped<IRepository<WebAppDatabaseContext>, Repository<WebAppDatabaseContext>>();
builder.Services.AddScoped<IFirestoreRepository, FirestoreRepository>();
builder.Services.AddScoped<ILoginService, LoginService>()
    .AddScoped<IUserService, UserService>()
    .AddScoped<ISpecialistProfileService, SpecialistProfileService>()
    .AddScoped<IRequestService, RequestService>()
    .AddScoped<IReplyService, ReplyService>()
    .AddScoped<ITransactionSummaryGenerator, TransactionSummaryGenerator>()
    .AddScoped<ICategoryService, CategoryService>()
    .AddScoped<IMailService, MailService>()
    .AddScoped<ISpecialistService, SpecialistService>()
    .AddScoped<IConversationService, ConversationService>()
    .AddScoped<IServiceTaskService, ServiceTaskService>()
    .AddScoped<IReviewService, ReviewService>()
    .AddScoped<IFirebaseStorageService, FirebaseStorageService>()
    .AddScoped<IPhotoService, PhotoService>()
    .AddScoped<IStripeAccountService, StripeAccountService>()
    .AddScoped<IPaymentService, PaymentService>()
    .AddScoped<IProtectionFeeConfigurationService, ProtectionFeeConfigurationService>()
    .AddScoped<ICustomerPaymentMethodService, CustomerPaymentMethodService>();
builder.Services.AddScoped<IConversationNotifier, ConversationNotifier>();
builder.Services.AddSingleton<IMessageUpdateQueue, MessageUpdateWorker>();
builder.Services.AddMemoryCache();

builder.Services.AddHostedService<InitializerWorker>()
    .AddHostedService<MessageUpdateWorker>();

builder.Services.Configure<JwtConfiguration>(
    builder.Configuration.GetSection("JwtConfiguration"));

builder.Services.AddSingleton(resolver =>
    resolver.GetRequiredService<IOptions<JwtConfiguration>>().Value);

ConfigureAuthentication();
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        // .RequireAuthenticatedUser()
        .RequireClaim(ClaimTypes.NameIdentifier)
        .RequireClaim(ClaimTypes.Name)
        .RequireClaim(ClaimTypes.Email)
        .RequireClaim(ClaimTypes.Role)
        .Build();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    Console.WriteLine($"Authorization header: {authHeader ?? "NOT PRESENT"}");
    
    if (authHeader != null && authHeader.StartsWith("Bearer "))
    {
        var token = authHeader.Substring("Bearer ".Length).Trim();
        Console.WriteLine($"Token extracted: {token.Substring(0, Math.Min(20, token.Length))}...");
    }
    
    await next();
});
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ConversationHub>("/hubs/conversations");

app.MapControllers();
app.MapFallbackToFile("browser/index.html");

await app.RunAsync();

void ConfigureAuthentication()
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme; // This is to use the JWT token with the "Bearer" scheme
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
{
    var jwtConfiguration = builder.Configuration.GetSection(nameof(JwtConfiguration)).Get<JwtConfiguration>();

    if (jwtConfiguration == null)
    {
        throw new InvalidOperationException("The JWT configuration needs to be set!");
    }

    Console.WriteLine($"JWT Config - Issuer: {jwtConfiguration.Issuer}, Audience: {jwtConfiguration.Audience}");
    Console.WriteLine($"JWT Key length: {jwtConfiguration.Key?.Length ?? 0}");

    if (jwtConfiguration.Key != null)
    {
        var key = Encoding.ASCII.GetBytes(jwtConfiguration.Key);
        options.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = jwtConfiguration.Audience,
            ValidIssuer = jwtConfiguration.Issuer,
            ClockSkew = TimeSpan.FromMinutes(5), // Temporarily increase this
            ValidateLifetime = true
        };
    }

    options.RequireHttpsMetadata = false;
    options.IncludeErrorDetails = true;
    
    // Enhanced debugging
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Token;
            Console.WriteLine($"OnMessageReceived - Token present: {!string.IsNullOrEmpty(token)}");
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"Token preview: {token.Substring(0, Math.Min(50, token.Length))}...");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("✅ Token validated successfully");
            var claims = context.Principal?.Claims?.ToList();
            Console.WriteLine($"Claims count: {claims?.Count ?? 0}");
            if (claims != null)
            {
                foreach (var claim in claims)
                {
                    Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                }
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"❌ Authentication failed: {context.Exception.Message}");
            Console.WriteLine($"Exception type: {context.Exception.GetType().Name}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {context.Exception.InnerException.Message}");
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"🔥 OnChallenge - Error: {context.Error}");
            Console.WriteLine($"Error description: {context.ErrorDescription}");
            Console.WriteLine($"Failure: {context.AuthenticateFailure?.Message}");
            return Task.CompletedTask;
        }
    };
});
    // }).AddJwtBearer(options =>
    // {
    //     var jwtConfiguration =
    //         builder.Configuration.GetSection(nameof(JwtConfiguration))
    //             .Get<JwtConfiguration>(); // Here we use the JWT configuration from the application.json.
    //
    //     if (jwtConfiguration == null)
    //     {
    //         throw new InvalidOperationException("The JWT configuration needs to be set!");
    //     }
    //
    //     var key = Encoding.ASCII.GetBytes(jwtConfiguration.Key); // Use configured key to verify the JWT signature.
    //     options.TokenValidationParameters = new()
    //     {
    //         ValidateIssuerSigningKey = true,
    //         IssuerSigningKey = new SymmetricSecurityKey(key),
    //         ValidateIssuer = true, // Validate the issuer claim in the JWT. 
    //         ValidateAudience = true, // Validate the audience claim in the JWT.
    //         ValidAudience = jwtConfiguration.Audience, // Sets the intended audience.
    //         ValidIssuer = jwtConfiguration.Issuer, // Sets the issuing authority.
    //         ClockSkew = TimeSpan
    //             .Zero // No clock skew is added, when the token expires it will immediately become unusable.
    //     };
    //     options.RequireHttpsMetadata = false;
    //     options.IncludeErrorDetails = true;
    // });
}