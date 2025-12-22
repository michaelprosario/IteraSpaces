using AppCore.Interfaces;
using AppCore.Services;
using AppInfra.Repositories;
using AppInfra.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Marten;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

// Initialize Firebase Admin SDK
// Note: Configure Firebase credentials in appsettings.json
var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"];
var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];

if (!string.IsNullOrEmpty(firebaseCredentialsPath) && File.Exists(firebaseCredentialsPath))
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(firebaseCredentialsPath),
        ProjectId = firebaseProjectId
    });
}
else
{
    // For development without Firebase credentials, log a warning
    Console.WriteLine("Warning: Firebase credentials not configured. Authentication will not work.");
}

// Add Authentication with Firebase JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// Configure Marten with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString!);
    
    // Configure schema auto-creation (use in development, not production)
    options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
    
    // Configure document mapping - Marten will use Id property as the document identity
    options.Schema.For<AppCore.Entities.User>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.Role>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.UserRole>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.Blog>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.UserLoginEvent>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.LeanSession>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.LeanParticipant>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.LeanTopic>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.LeanTopicVote>().Identity(x => x.Id);
    options.Schema.For<AppCore.Entities.LeanSessionNote>().Identity(x => x.Id);
    
    // Add indexes for commonly queried fields
    options.Schema.For<AppCore.Entities.User>()
        .Index(x => x.Email)
        .Index(x => x.FirebaseUid);
    
    options.Schema.For<AppCore.Entities.Role>()
        .Index(x => x.Name);
    
    options.Schema.For<AppCore.Entities.UserRole>()
        .Index(x => x.UserId)
        .Index(x => x.RoleId);
    
    options.Schema.For<AppCore.Entities.LeanSession>()
        .Index(x => x.FacilitatorUserId)
        .Index(x => x.Status);
    
    options.Schema.For<AppCore.Entities.LeanParticipant>()
        .Index(x => x.LeanSessionId)
        .Index(x => x.UserId);
    
    options.Schema.For<AppCore.Entities.LeanTopic>()
        .Index(x => x.LeanSessionId)
        .Index(x => x.Status);
    
    options.Schema.For<AppCore.Entities.LeanTopicVote>()
        .Index(x => x.LeanTopicId)
        .Index(x => x.LeanSessionId);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200", 
                "https://localhost:4200",
                "https://congenial-parakeet-v65x4jgr6p2v96-4200.app.github.dev")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // Required for SignalR
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register AppCore Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IUsersQueryService, UsersQueryService>();
builder.Services.AddScoped<LeanSessionService>();
builder.Services.AddScoped<LeanParticipantService>();
builder.Services.AddScoped<LeanTopicService>();
builder.Services.AddScoped<LeanSessionQueryService>();

// Register Generic Entity Services
builder.Services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));
builder.Services.AddScoped(typeof(IRepository<>), typeof(AppInfra.Repositories.Repository<>));

// Register AppInfra Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IUsersQueryRepository, AppInfra.Repositories.UsersQueryRepository>();
builder.Services.AddScoped<ILeanSessionRepository, AppInfra.Repositories.LeanSessionRepository>();
builder.Services.AddScoped<ILeanParticipantRepository, AppInfra.Repositories.LeanParticipantRepository>();
builder.Services.AddScoped<ILeanTopicRepository, AppInfra.Repositories.LeanTopicRepository>();
builder.Services.AddScoped<ILeanTopicVoteRepository, AppInfra.Repositories.LeanTopicVoteRepository>();
builder.Services.AddScoped<ILeanSessionNoteRepository, AppInfra.Repositories.LeanSessionNoteRepository>();

// Register AppInfra External Services
builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthenticationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IteraSpaces API V1");
    });
}

app.UseHttpsRedirection();

// CORS must come before Authentication and Authorization
app.UseCors("AllowAngular");

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR Hub
app.MapHub<IteraWebApi.Hubs.LeanSessionHub>("/hubs/lean-session");

app.Run();
