using Microsoft.EntityFrameworkCore;
using WHY.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<WHYBotDbContext>(connectionName: "postgresdb");

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using var scope = app.Services.CreateScope();
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<WHYBotDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
