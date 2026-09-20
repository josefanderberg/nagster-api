using Microsoft.EntityFrameworkCore;
using NagsterApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SQLite: hela databasen är en fil som skapas automatiskt.
builder.Services.AddDbContext<NagsterContext>(options =>
    options.UseSqlite("Data Source=nagster.db"));

// CORS: webbappen kör på en annan port och måste få anropa API:et.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Skapa databasen vid första starten – ingen manuell setup behövs.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NagsterContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Serverar uppladdade filer från wwwroot, t.ex. /uploads/xyz.webm
app.UseStaticFiles();

app.MapControllers();

app.Run();
