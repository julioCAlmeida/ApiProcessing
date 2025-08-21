using Api.Data;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Processing API", Version = "v1" });
});

var connString =
    builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"] 
    ?? builder.Configuration["DB_CONN"]                              
    ?? Environment.GetEnvironmentVariable("DB_CONN");                

if (string.IsNullOrWhiteSpace(connString))
{
    throw new InvalidOperationException(
        "Connection string não configurada. Defina 'ConnectionStrings:DefaultConnection' " +
        "ou a variável 'DB_CONN' (appsettings/appsettings.Local.json/User-Secrets/Docker/env).");
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connString));

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        // enums como "pending", "running", "completed", "failed"
        o.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.AddSingleton<ScriptExecutor>();
builder.Services.AddHostedService<ProcessingQueue>();
builder.Services.AddSingleton<ScriptValidationService>();

var app = builder.Build();

app.UseSwagger(c => { c.RouteTemplate = "openapi/{documentName}.json"; });
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/openapi/v1.json", "Processing API v1");
    c.RoutePrefix = "docs"; 
});

app.MapGet("/openapi.json", () => Results.Redirect("/openapi/v1.json")).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
