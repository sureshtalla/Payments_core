
using Payments_core.Services.DataLayer;
using Payments_core.Services.MasterDataService;
using Payments_core.Services.UserDataService;

namespace Payments_core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register your services
            builder.Services.AddSingleton<IDapperContext, DapperContext>();
            builder.Services.AddScoped<IMasterDataService, MasterDataService>();
            builder.Services.AddScoped<IUserDataService, UserDataService>();
            builder.Services.AddScoped<IOtpService, OtpService>();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
