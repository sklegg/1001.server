using System.Text.Json;
using Server1001.Services;
using Server1001.Shared;
using Server1001.Shared.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(json => {
        json.IncludeScopes = true;
        json.TimestampFormat = null;
        json.UseUtcTimestamp = false;
        json.JsonWriterOptions = new JsonWriterOptions{
            Indented = false
        };
    });

builder.Services.AddControllers();
builder.Services.AddCors(c => {
    c.AddPolicy("LocalCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5050",
            "http://localhost:5000",
            "https://dev.1001songsgenerator.com",
            "https://www.1001songsgenerator.com").AllowCredentials();
        policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(origin => true);
    });
});

builder.Services.AddScoped<IEmailService, EmailService>()
    .AddScoped<IUserService, UserService>()
    .AddScoped<ISongService, SongService>()
    .AddScoped<IReviewService, ReviewService>()
    .AddSingleton<IRepository, Repository>()
    .AddSingleton<IDynamoRepository, DynamoRepository>()
    .AddSingleton<IJwtUtils, JwtUtils>()
    .AddSingleton<ICustomConfiguration, CustomConfiguration>();

var app = builder.Build();
app.UseCors("LocalCorsPolicy");

app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<JwtMiddleware>();
app.Run();