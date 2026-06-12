using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opt =>
    {
        opt.Authority = builder.Configuration["Auth:Authority"];
        opt.Audience = builder.Configuration["Auth:Audience"];
        opt.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddCors(o =>
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins(
            builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:4200")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials()));

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();

app.Run();
