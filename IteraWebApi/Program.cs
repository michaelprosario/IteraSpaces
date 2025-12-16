using AppCore.Interfaces;
using AppCore.Services;
using AppInfra.Data;
using AppInfra.Repositories;
using AppInfra.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

// Configure Entity Framework Core with PostgreSQL
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        dataSource,
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("IteraWebApi")
    ));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200", 
                "https://localhost:4200",
                "https://congenial-parakeet-v65x4jqr6p2v96-4200.app.github.dev")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
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

// Register AppInfra Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();

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

app.Run();
