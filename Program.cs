using Microsoft.EntityFrameworkCore;
using RunCutWeb.Application.Interfaces;
using RunCutWeb.Infrastructure.Data;
using RunCutWeb.Infrastructure.Services;

namespace RunCutWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add Entity Framework with performance optimizations
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                {
                    sqlOptions.CommandTimeout(300); // 5 minutes timeout for large imports
                    sqlOptions.EnableRetryOnFailure();
                })
                .EnableSensitiveDataLogging(false)
                .EnableServiceProviderCaching());

            // Register application services
            builder.Services.AddScoped<IDataImportService, DataImportService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}
