using Libraries.Kafka;
using Libraries.NpgsqlService;
using Libraries.Web.Common.Caching;
using Libraries.Web.Common.Middlewares;
using Libraries.Web.Common.Settings;
using Libraries.Web.Common.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using UserService.API.Services;
using UserService.Database;

namespace UserService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddOptions();
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("KafkaSettings"));
            builder.Services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
#if DEBUG
                o.RequireHttpsMetadata = false;
#endif
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
            builder.Services.AddRateLimiter(o =>
            {
                o.AddFixedWindowLimiter("LoginPolicy", c =>
                {
                    c.PermitLimit = 10;
                    c.Window = TimeSpan.FromMinutes(1);
                    c.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    c.QueueLimit = 0;
                });
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
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
            builder.Services.AddStackExchangeRedisCache(options =>
            {
#if DEBUG
                options.Configuration = builder.Configuration.GetConnectionString("redis_debug");
#else
                options.Configuration = builder.Configuration.GetConnectionString("redis");
#endif
            });
            builder.Services.AddSingleton<INpgsqlService, NpgsqlService>();
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var connStr = builder.Configuration.GetConnectionString("redis");
                return ConnectionMultiplexer.Connect(connStr!);
            });
            builder.Services.AddSingleton<IDistributedLock, RedisLock>();
            builder.Services.AddTransient<UsersService>();
            builder.Services.AddTransient<IFriendService, FriendService>();
            builder.Services.AddTransient<IPostRepository, PostRepository>();
            builder.Services.AddTransient<PostService>();
            var app = builder.Build();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}