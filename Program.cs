using Microsoft.EntityFrameworkCore;
using NagsterApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SQLite: hela databasen är en fil som skapas automatiskt.
builder.Services.AddDbContext<NagsterContext>(options =>
    options.UseSqlite("Data Source=nagster.db"));

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

// Serverar uppladdade filer från wwwroot, t.ex. /uploads/xyz.webm
app.UseStaticFiles();

app.MapControllers();

app.Run();
