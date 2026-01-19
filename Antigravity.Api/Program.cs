

using Microsoft.EntityFrameworkCore;
using Antigravity.Api.Data;

// Fix for Npgsql DateTime Kind=Unspecified error
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://192.168.1.148:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

builder.Services.AddDbContext<FontaneriaContext>(options =>
{
    if (databaseProvider == "PostgreSql")
    {
        var postgresConnection = builder.Configuration.GetConnectionString("PostgresConnection");
        options.UseNpgsql(postgresConnection);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// Repositories & Services
builder.Services.AddScoped<Antigravity.Api.Repositories.IFontaneriaRepository, Antigravity.Api.Repositories.FontaneriaRepository>();
builder.Services.AddScoped<Antigravity.Api.Services.ISimpleMapper, Antigravity.Api.Services.SimpleMapper>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAngularApp");
app.UseAuthorization();
app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
