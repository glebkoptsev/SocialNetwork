using DialogService.API.Services;
using Libraries.NpgsqlService;
using Libraries.Web.Common.Clients;
using Libraries.Web.Common.Middlewares;
using Libraries.Web.Common.Settings;
using Libraries.Web.Common.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Npgsql;
using NpgsqlTypes;

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

            builder.Services.AddHttpClient<UserServiceClient>();
            builder.Services.AddTransient<UserServiceClient>();
            builder.Services.AddSingleton<NpgsqlService>();
            builder.Services.AddSingleton<IChatService, RedisChatService>();
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
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Ensure dialog tables exist
            var npgsql = app.Services.GetRequiredService<NpgsqlService>();
            await npgsql.ExecuteNonQueryAsync("""
                CREATE TABLE IF NOT EXISTS public.chats
                (
                    chat_id uuid NOT NULL,
                    chat_name character varying(50) NOT NULL,
                    creator_id uuid NOT NULL,
                    creation_datetime timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    last_update_datetime timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT chats_pkey PRIMARY KEY (chat_id)
                );
                CREATE TABLE IF NOT EXISTS public.chat_users
                (
                    chat_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    creation_datetime timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT chat_users_pkey PRIMARY KEY (chat_id, user_id),
                    CONSTRAINT chat_users_chat_id_fkey FOREIGN KEY (chat_id) REFERENCES public.chats (chat_id)
                );
                CREATE TABLE IF NOT EXISTS public.messages
                (
                    message_id uuid NOT NULL,
                    chat_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    message character varying(2000) NOT NULL,
                    creation_datetime timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT messages_pkey PRIMARY KEY (message_id, chat_id)
                )
                """, []);

            app.Run();
        }
    }
}
