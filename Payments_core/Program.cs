using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Payments_core.Services.BBPSService;
using Payments_core.Services.BBPSService.Repository;
using Payments_core.Services.DataLayer;
using Payments_core.Services.MasterDataService;
using Payments_core.Services.MerchantDataService;
using Payments_core.Services.OTPService;
using Payments_core.Services.PricingMDRDataService;
using Payments_core.Services.SuperDistributorService;
using Payments_core.Services.UserDataService;
using Payments_core.Services.WalletService;

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
            builder.Services.AddSingleton<GoogleDriveService>();   // single instance for Drive API

            // Master data service
            builder.Services.AddScoped<IMasterDataService, MasterDataService>();

            // User data service (this fixes your error)
            builder.Services.AddScoped<IUserDataService, UserDataService>();

            // OTP service (needed for verify-otp endpoint)
            builder.Services.AddScoped<IOtpService, OtpService>();
            builder.Services.AddScoped<ISuperDistributorService, SuperDistributorService>();
            builder.Services.AddScoped<IMerchantDataService, MerchantDataService>();
            builder.Services.AddScoped<IPricingMDRDataService, PricingMDRDataService>();
            builder.Services.AddScoped<IMSG91OTPService, MSG91OTPService>();

            builder.Services.AddScoped<SuperDistributorService>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // BillAvenue service (BBPS)
            builder.Services.AddScoped<IBbpsRepository, BbpsRepository>();
            builder.Services.AddHttpClient<IBillAvenueClient, BillAvenueClient>();
            builder.Services.AddScoped<IBbpsService, BbpsService>();
            //builder.Services.AddHostedService<BbpsStatusRequeryJob>();
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<IBbpsComplaintService, BbpsComplaintService>();
            builder.Services.AddScoped<IBbpsRepository, BbpsRepository>();

            // === CORS ===
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowRCPWClient", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:4200",
                            "https://merchant.fastcashfnx.in"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // (JWT config can stay commented for now if you’re not using it)
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
                };
            });

            var app = builder.Build();

            // Static files (if you serve anything from wwwroot)
            app.UseStaticFiles();

            // ✅ UPDATED: Return clean JSON for duplicates + real server errors
            app.Use(async (context, next) =>
            {
                try
                {
                    var requestId = context.TraceIdentifier;

                    Console.WriteLine("----- INCOMING REQUEST -----");
                    Console.WriteLine($"RequestId : {requestId}");
                    Console.WriteLine($"Path      : {context.Request.Path}");
                    Console.WriteLine($"Method    : {context.Request.Method}");

                    await next();

                    Console.WriteLine($"Response Status : {context.Response.StatusCode}");
                    Console.WriteLine("----- REQUEST END -----");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unhandled Exception:");
                    Console.WriteLine(ex);

                    var rawMsg = (ex.InnerException?.Message ?? ex.Message ?? "").ToLowerInvariant();

                    int statusCode = StatusCodes.Status500InternalServerError;
                    string code = "SERVER_ERROR";
                    string message = "We couldn't complete your request right now. Please try again.";
                    string? field = null;

                    // ✅ Detect duplicates and return 409 with exact field message
                    if (rawMsg.Contains("duplicate") ||
                        rawMsg.Contains("duplicate entry") ||
                        rawMsg.Contains("already exists") ||
                        rawMsg.Contains("unique") ||
                        rawMsg.Contains("cannot insert duplicate key"))
                    {
                        statusCode = StatusCodes.Status409Conflict;
                        code = "DUPLICATE_ENTRY";
                        message = "This record already exists. Please use different details.";

                        // Identify which field (based on error text / unique index name)
                        if (rawMsg.Contains("pan") || rawMsg.Contains("uq_") && rawMsg.Contains("pan"))
                        {
                            code = "DUPLICATE_PAN";
                            message = "PAN number already exists.";
                            field = "pan";
                        }
                        else if (rawMsg.Contains("aadhaar") || rawMsg.Contains("aadhar") ||
                                 rawMsg.Contains("uq_") && (rawMsg.Contains("aadhaar") || rawMsg.Contains("aadhar")))
                        {
                            code = "DUPLICATE_AADHAAR";
                            message = "Aadhaar number already exists.";
                            field = "aadhaar";
                        }
                        else if (rawMsg.Contains("email") || rawMsg.Contains("uq_") && rawMsg.Contains("email"))
                        {
                            code = "DUPLICATE_EMAIL";
                            message = "Email already exists.";
                            field = "email";
                        }
                        else if (rawMsg.Contains("mobile") || rawMsg.Contains("phone") || rawMsg.Contains("contact") ||
                                 rawMsg.Contains("uq_") && rawMsg.Contains("mobile"))
                        {
                            code = "DUPLICATE_MOBILE";
                            message = "Mobile number already exists.";
                            field = "mobile";
                        }
                    }

                    context.Response.StatusCode = statusCode;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        code,
                        message,
                        field,
                        traceId = context.TraceIdentifier
                    });
                }
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowRCPWClient");

            app.UseHttpsRedirection();

            // Enable Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
