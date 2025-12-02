
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Payments_core.Services.DataLayer;
using Payments_core.Services.MasterDataService;
using Payments_core.Services.UserDataService;
using System.Text;
//using Payments_core.Services.OtpDataService; 

namespace Payments_core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // === DATA LAYER & SERVICES REGISTRATION ===

            // Dapper context should be scoped (per request), not singleton
            builder.Services.AddScoped<IDapperContext, DapperContext>();

            // Master data service
            builder.Services.AddScoped<IMasterDataService, MasterDataService>();

            // User data service (this fixes your error)
            builder.Services.AddScoped<IUserDataService, UserDataService>();

            // OTP service (needed for verify-otp endpoint)
            builder.Services.AddScoped<IOtpService, OtpService>();
            // === CORS ===
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowRCPWClient", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:4200"   // Angular dev
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // (JWT config can stay commented for now if you’re not using it)
            /*
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtSettings = builder.Configuration.GetSection("JwtSettings");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Key"]))
                };
            });
            */

            var app = builder.Build();

            // Static files (if you serve anything from wwwroot)
            app.UseStaticFiles();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowRCPWClient");

            app.UseHttpsRedirection();

            // app.UseAuthentication(); // uncomment when JWT is enabled
            app.UseAuthorization();

            app.MapControllers();

            app.Run();

        }
    }
}
