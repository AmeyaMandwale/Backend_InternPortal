using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using InternConnect_Backend.Data;
using InternConnect_Backend.Services;

namespace Backend_InternPortal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ? Load environment variables
            DotNetEnv.Env.Load();
            string databaseUrl = Environment.GetEnvironmentVariable("DATABASE_STRING");

            builder.Configuration["ConnectionStrings:DefaultConnection"] = databaseUrl;

            // ? JWT secret key from env or appsettings
            string jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET")
                            ?? builder.Configuration["Jwt:Key"];

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpClient(); // added new
            builder.Services.AddHttpClient<SerpApiService>();


            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5156); // HTTP
                //options.ListenAnyIP(7031, listenOptions => // HTTPS
                //{
                //    listenOptions.UseHttps();
                //});
            });
            // ? Database config
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    databaseUrl,
                    new MySqlServerVersion(new Version(8, 0, 32)) // adjust to your MySQL version
                )
            );

            // ? JWT Authentication config
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            var app = builder.Build();

            // ? Middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowFrontend");
            


           // app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication(); // ?? MUST be before UseAuthorization
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
