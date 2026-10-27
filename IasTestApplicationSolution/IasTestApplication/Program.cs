using IasTestApplication.Data;
using IasTestApplication.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Archivo SQLite dentro del proyecto (App_Data/), configurable en appsettings.json.
var dbPath = Path.GetFullPath(
    builder.Configuration["Database:Path"] ?? "App_Data/campaign.db",
    builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<ISaleService, SaleService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
    o.SwaggerDoc("v1", new() { Title = "Venta de seguros de campaña", Version = "v1" }));

var app = builder.Build();

// Crea y puebla la base al arrancar. Restablecer: dotnet run -- --Database:Reset=true
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseInitializer.InitializeAsync(db, app.Configuration.GetValue<bool>("Database:Reset"), logger);
}

// Swagger solo en Development: JSON en /swagger/v1/swagger.json, UI en /swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();

app.Run();
