using DialogService.API.Services;
using DialogService.Database;
using Libraries.Web.Common.Caching;
using Libraries.Web.Common.Clients;
using Libraries.Web.Common.Http;
using Libraries.Web.Common.Middlewares;
using Libraries.Web.Common.Settings;
using Libraries.Web.Common.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace DialogService.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddOptions();
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            builder.Services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                if (builder.Environment.IsDevelopment())
                    o.RequireHttpsMetadata = false;
                o.SaveToken = true;
                var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings!.Issuer,
                    ValidAudience = jwtSettings!.Audience,
                    IssuerSigningKey = jwtSettings.GetSigningCredentials().Key
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo { Title = "Otus homework", Version = "v1" });
                o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = HeaderNames.Authorization,
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = JwtBearerDefaults.AuthenticationScheme
                });
                o.OperationFilter<SecurityRequirementFilter>(JwtBearerDefaults.AuthenticationScheme);
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<ForwardTokenHandler>();
            var userServiceUrl = builder.Configuration.GetValue<string>("UserService:URL") ?? "http://user_api:8080";
            builder.Services.AddHttpClient<UserServiceClient>(c =>
            {
                c.BaseAddress = new Uri(userServiceUrl);
            }).AddHttpMessageHandler<ForwardTokenHandler>();
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var connStr = builder.Configuration.GetConnectionString("redis");
                return ConnectionMultiplexer.Connect(connStr!);
            });
            builder.Services.AddSingleton<IDistributedLock, RedisLock>();
            builder.Services.AddSingleton<RedisChatService>();
            builder.Services.AddSingleton<IChatService>(sp => sp.GetRequiredService<RedisChatService>());
            builder.Services.AddDbContextPool<DialogDbContext>(options =>
            {
                var connStr = builder.Configuration.GetConnectionString("postgres");
                options.UseNpgsql(connStr!).UseSnakeCaseNamingConvention();
            });
            var corsOrigins = builder.Configuration["CORS:AllowedOrigins"]?.Split(",", StringSplitOptions.RemoveEmptyEntries) ?? ["http://localhost:3000"];
            builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
                p.WithOrigins(corsOrigins)
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                    .WithHeaders("authorization", "content-type", "x-requested-with")
                    .AllowCredentials()));
            var app = builder.Build();
            app.UseCors("Frontend");
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Apply migrations
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
                await context.Database.MigrateAsync();
            }

            app.Run();
        }
    }
}
