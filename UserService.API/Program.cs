using Libraries.Kafka;
using Libraries.NpgsqlService;
using Libraries.NpgsqlService.Security;
using Libraries.Web.Common.Caching;
using Libraries.Web.Common.Middlewares;
using Libraries.Web.Common.Settings;
using Libraries.Web.Common.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Npgsql;
using NpgsqlTypes;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using UserService.API.Services;
using UserService.Database;

namespace UserService.API
{
    public class Program
    {
        public static async Task Main(string[] args)
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
            builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
                p.WithOrigins("http://localhost:3000")
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                    .WithHeaders("authorization", "content-type", "x-requested-with")
                    .AllowCredentials()));
            var app = builder.Build();
            app.UseCors("Frontend");
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Ensure database schema exists
            var npgsql = app.Services.GetRequiredService<INpgsqlService>();
            await npgsql.ExecuteNonQueryAsync("""
                CREATE TABLE IF NOT EXISTS public.users
                (
                    user_id uuid NOT NULL,
                    first_name character varying(30) NOT NULL,
                    second_name character varying(30) NOT NULL,
                    birthdate character varying(11) NOT NULL,
                    biography character varying(1000) NOT NULL,
                    city character varying(255) NOT NULL,
                    password character varying(255) NOT NULL,
                    can_publish_messages bool not null default false,
                    login character varying(50) NOT NULL,
                    CONSTRAINT pk_users PRIMARY KEY (user_id)
                );
                CREATE INDEX IF NOT EXISTS users_fname_sname_idx ON public.users(first_name varchar_pattern_ops, second_name varchar_pattern_ops);
                CREATE TABLE IF NOT EXISTS public.friends
                (
                    user_id uuid,
                    friend_id uuid,
                    PRIMARY KEY(user_id, friend_id),
                    FOREIGN KEY (user_id) REFERENCES users (user_id),
                    FOREIGN KEY (friend_id) REFERENCES users (user_id)
                );
                CREATE TABLE IF NOT EXISTS public.posts
                (
                    post_id uuid not null,
                    user_id uuid not null,
                    post varchar(2000) not null,
                    creation_datetime timestamp not null default CURRENT_TIMESTAMP,
                    PRIMARY KEY(post_id),
                    FOREIGN KEY (user_id) REFERENCES users (user_id)
                );
                CREATE INDEX IF NOT EXISTS posts_userid_idx ON public.posts(user_id);
                CREATE TABLE IF NOT EXISTS public.feed_outbox
                (
                    id BIGSERIAL PRIMARY KEY,
                    kafka_key TEXT NOT NULL,
                    kafka_value TEXT NOT NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    processed_at TIMESTAMPTZ
                )
                """, []);

            // Add login column if upgrading from old schema
            await npgsql.ExecuteNonQueryAsync("""
                ALTER TABLE public.users ADD COLUMN IF NOT EXISTS login VARCHAR(50) NOT NULL DEFAULT '';
                CREATE UNIQUE INDEX IF NOT EXISTS users_login_idx ON public.users(login);
                """, []);

            // Create system user for internal services
            var systemUserId = new Guid("00000000-0000-0000-0000-000000000000");
            await npgsql.ExecuteNonQueryAsync("""
                INSERT INTO public.users (user_id, first_name, second_name, birthdate, biography, city, password, login)
                VALUES (@Id, 'System', 'User', '', '', '', @Password, 'system')
                ON CONFLICT (user_id) DO NOTHING
                """, [
                new NpgsqlParameter("Id", NpgsqlDbType.Uuid) { Value = systemUserId },
                new NpgsqlParameter("Password", NpgsqlDbType.Varchar) { Value = PasswordHasher.Hash("placeholder") }
            ]);
            // Update login for system user if row already existed before login column was added
            await npgsql.ExecuteNonQueryAsync("""
                UPDATE public.users SET login = 'system' WHERE user_id = @Id AND login = ''
                """, [
                new NpgsqlParameter("Id", NpgsqlDbType.Uuid) { Value = systemUserId }
            ]);

            app.Run();
        }
    }
}