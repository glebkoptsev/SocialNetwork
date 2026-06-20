using Libraries.Web.Common.Middlewares;
using Libraries.Web.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR().AddStackExchangeRedis(builder.Configuration.GetConnectionString("redis")!);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration["CORS:AllowedOrigins"]?.Split(",", StringSplitOptions.RemoveEmptyEntries)
    ?? ["http://localhost:3000"];
builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
    p.WithOrigins(corsOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
        .WithHeaders("authorization", "content-type", "x-requested-with")
        .AllowCredentials()));

builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<FeedHub>("/post/feed/posted");
app.MapHealthChecks("/health");

app.Run();
