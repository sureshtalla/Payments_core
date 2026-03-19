using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Payments_core.Integrations.Cashfree;
using Payments_core.Models.Settings;
using Payments_core.Services.BankService;
using Payments_core.Services.BBPSService;
using Payments_core.Services.BBPSService.Repository;
using Payments_core.Services.DataLayer;
using Payments_core.Services.FailureQueue;
using Payments_core.Services.FileStorage;
using Payments_core.Services.Jobs;
using Payments_core.Services.KycVerificationService;
using Payments_core.Services.MasterDataService;
using Payments_core.Services.MerchantDataService;
using Payments_core.Services.Monitoring;
using Payments_core.Services.OTPService;
using Payments_core.Services.Payments;
using Payments_core.Services.PricingMDRDataService;
using Payments_core.Services.Reconciliation;
using Payments_core.Services.Security;
using Payments_core.Services.SuperDistributorService;
using Payments_core.Services.UserDataService;
using Payments_core.Services.WalletService;
using System.Text;
using System.Threading.RateLimiting;

namespace Payments_core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Read Environment Variables → override appsettings placeholders ─
            void SetConfig(string envVar, string configKey)
            {
                var val = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(val))
                    builder.Configuration[configKey] = val;
            }

            SetConfig("DB_CONNECTION_STRING", "ConnectionStrings:SqlConnection");
            SetConfig("JWT_SECRET_KEY", "JwtSettings:Key");
            SetConfig("JWT_ISSUER", "JwtSettings:Issuer");
            SetConfig("JWT_AUDIENCE", "JwtSettings:Audience");
            SetConfig("FRONTEND_URL", "PaymentSettings:FrontendUrl");
            SetConfig("WEBHOOK_URL", "PaymentSettings:WebhookUrl");
            SetConfig("ALLOWED_HOSTS", "AllowedHosts");
            SetConfig("CASHFREE_BASE_URL", "Cashfree:BaseUrl");
            SetConfig("CASHFREE_DIGILOCKER_REDIRECT", "Cashfree:DigiLockerRedirectUrl");
            SetConfig("BILLAVENUE_BASE_URL", "BillAvenue:BaseUrl");
            SetConfig("BILLAVENUE_ACCESS_CODE", "BillAvenue:AccessCode");
            SetConfig("BILLAVENUE_WORKING_KEY", "BillAvenue:WorkingKey");
            SetConfig("BILLAVENUE_INSTITUTE_ID", "BillAvenue:InstituteId");
            SetConfig("BILLAVENUE_AGENT_ID", "BillAvenue:AgentId");
            SetConfig("CASHFREE_WEBHOOK_SECRET", "Webhook:CashfreeSecret");
            SetConfig("RAZORPAY_WEBHOOK_SECRET", "Webhook:RazorpaySecret");
            // ── End Environment Variables ──────────────────────────────────────

            // ── Controllers & Swagger ──────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ── CORS ───────────────────────────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FinxPolicy", policy =>
                    policy.WithOrigins(
                            "http://localhost:4200",
                            "https://merchant.fastcashfnx.in",
                            "https://www.fuzioniq.in",
                            "https://fuzioniq.in"
                          )
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
            });

            // ── Rate Limiting ──────────────────────────────────────────────────
            builder.Services.AddRateLimiter(options =>
            {
                options.AddSlidingWindowLimiter("login", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.SegmentsPerWindow = 2;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                options.AddSlidingWindowLimiter("otp", opt =>
                {
                    opt.PermitLimit = 3;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.SegmentsPerWindow = 2;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                options.AddSlidingWindowLimiter("payment", opt =>
                {
                    opt.PermitLimit = 30;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.SegmentsPerWindow = 4;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                options.AddSlidingWindowLimiter("general", opt =>
                {
                    opt.PermitLimit = 120;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.SegmentsPerWindow = 4;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 5;
                });

                options.RejectionStatusCode = 429;
            });

            // ── Data Layer & Services ──────────────────────────────────────────
            builder.Services.AddScoped<IDapperContext, DapperContext>();
            builder.Services.AddScoped<IMasterDataService, MasterDataService>();
            builder.Services.AddScoped<IUserDataService, UserDataService>();
            builder.Services.AddScoped<IOtpService, OtpService>();
            builder.Services.AddScoped<ISuperDistributorService, SuperDistributorService>();
            builder.Services.AddScoped<IMerchantDataService, MerchantDataService>();
            builder.Services.AddScoped<IPricingMDRDataService, PricingMDRDataService>();
            builder.Services.AddScoped<IMSG91OTPService, MSG91OTPService>();
            builder.Services.AddScoped<SuperDistributorService>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            builder.Services.AddHttpClient<IBillAvenueClient, BillAvenueClient>();
            builder.Services.AddScoped<IBbpsService, BbpsService>();
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<IBbpsComplaintService, BbpsComplaintService>();
            builder.Services.AddScoped<IBbpsRepository, BbpsRepository>();
            builder.Services.AddScoped<ILocalFileService, LocalFileService>();
            builder.Services.AddSingleton<WebhookSignatureService>();
            builder.Services.AddScoped<ReconciliationService>();
            builder.Services.AddHostedService<ReconciliationJob>();
            builder.Services.AddScoped<AuditService>();
            builder.Services.AddScoped<IdempotencyService>();
            builder.Services.AddScoped<FailureService>();
            builder.Services.AddScoped<PgRetryService>();
            builder.Services.AddScoped<FraudService>();
            builder.Services.AddScoped<MetricsService>();

            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient<CashfreeGateway>();
            builder.Services.AddHttpClient<EasebuzzGateway>();
            builder.Services.AddScoped<IPaymentGateway, EasebuzzGateway>();
            builder.Services.AddScoped<IPaymentGateway, CashfreeGateway>();
            builder.Services.AddScoped<IKycVerificationService, KycVerificationService>();
            builder.Services.AddScoped<KycApiCredentialService>();
            builder.Services.AddHttpClient<CashfreeVerificationClient>();
            builder.Services.AddScoped<PaymentRouterService>();
            builder.Services.AddScoped<IBankService, BankService>();

            builder.Services.Configure<PaymentSettings>(
                builder.Configuration.GetSection("PaymentSettings"));

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
            });

            // ── JWT Authentication ─────────────────────────────────────────────
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
                                                 Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // ── Build App ──────────────────────────────────────────────────────
            var app = builder.Build();

            // ── Security Headers ───────────────────────────────────────────────
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "no-referrer");

                if (app.Environment.IsDevelopment())
                {
                    context.Response.Headers.Append(
                        "Content-Security-Policy",
                        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:");
                }
                else
                {
                    context.Response.Headers.Append(
                        "Content-Security-Policy",
                        "default-src 'self'; frame-ancestors 'none'");
                }

                if (!app.Environment.IsDevelopment())
                {
                    context.Response.Headers.Append(
                        "Strict-Transport-Security",
                        "max-age=31536000; includeSubDomains");
                }

                await next();
            });

            // ── Global Exception Handler ───────────────────────────────────────
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    var rawMsg = (ex.InnerException?.Message ?? ex.Message ?? "")
                                 .ToLowerInvariant();

                    int statusCode = StatusCodes.Status500InternalServerError;
                    string code = "SERVER_ERROR";
                    string message = "We couldn't complete your request right now. Please try again.";
                    string? field = null;

                    if (rawMsg.Contains("duplicate") ||
                        rawMsg.Contains("already exists") ||
                        rawMsg.Contains("unique") ||
                        rawMsg.Contains("cannot insert duplicate key"))
                    {
                        statusCode = StatusCodes.Status409Conflict;
                        code = "DUPLICATE_ENTRY";
                        message = "This record already exists. Please use different details.";

                        if (rawMsg.Contains("pan"))
                        {
                            code = "DUPLICATE_PAN"; message = "PAN number already exists."; field = "pan";
                        }
                        else if (rawMsg.Contains("aadhaar") || rawMsg.Contains("aadhar"))
                        {
                            code = "DUPLICATE_AADHAAR"; message = "Aadhaar number already exists."; field = "aadhaar";
                        }
                        else if (rawMsg.Contains("email"))
                        {
                            code = "DUPLICATE_EMAIL"; message = "Email already exists."; field = "email";
                        }
                        else if (rawMsg.Contains("mobile") || rawMsg.Contains("phone"))
                        {
                            code = "DUPLICATE_MOBILE"; message = "Mobile number already exists."; field = "mobile";
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

            // ── Swagger — DEV ONLY ─────────────────────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseCors("FinxPolicy");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}