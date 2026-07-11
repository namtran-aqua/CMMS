using CMMS.Server;
using CMMS.Server.Services.DailyJobService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Dapper;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddHostedService<EquipmentStatusUpdateBackgroundService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "VerySecretKey12345"))
        };
    });
builder.Services.AddAppServices();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CMMS API",
        Version = "v1"
    });
});

var app = builder.Build();

// Run DB migrations/updates on startup
using (var scope = app.Services.CreateScope())
{
    var connectionFactory = scope.ServiceProvider.GetRequiredService<CMMS.Data.Connection.ISqlConnectionFactory>();
    try
    {
        using var connection = connectionFactory.CreateConnection();
        // Ensure MovementType column exists (self-healing)
        connection.Execute(@"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tbl_Transactions') AND name = 'MovementType')
            BEGIN
                ALTER TABLE dbo.Tbl_Transactions ADD MovementType NVARCHAR(50) NULL;
            END
        ");

        // Migrate existing MAINTENANCE transactions to OUT and set MovementType
        connection.Execute(@"
            UPDATE dbo.Tbl_Transactions
            SET Type = 'OUT', MovementType = 'MAINTENANCE'
            WHERE Type = 'MAINTENANCE';

            UPDATE dbo.Tbl_Transactions
            SET MovementType = 'ADJUST'
            WHERE Type = 'IN' AND MovementType IS NULL;

            UPDATE dbo.Tbl_Transactions
            SET MovementType = 'ADJUST'
            WHERE Type = 'OUT' AND MovementType IS NULL;
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error running startup migration: {ex.Message}");
    }
}

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";         
    c.SwaggerEndpoint(
        "v1/swagger.json",
        "CMMS API v1"
    );
});

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");
app.MapRazorPages();
app.MapControllers();

app.MapFallbackToFile("index.html");
app.Run();

