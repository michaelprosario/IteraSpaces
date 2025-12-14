using AppCore.Interfaces;
using AppCore.Services;
using AppInfra.Repositories;
using AppInfra.Services;

var builder = WebApplication.CreateBuilder(args);

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

// Register AppInfra Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

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

// Add Authorization middleware (for future JWT implementation)
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
