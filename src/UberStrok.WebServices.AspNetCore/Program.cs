using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace UberStrok.WebServices.AspNetCore
{
    public class Program
    {
        private static Startup startup;
        public static void Main(string[] args)
        {
            var app = CreateWebHostBuilder(args).Build();
            startup.Configure(app, app.Environment);
            app.Run();
        }

        public static WebApplicationBuilder CreateWebHostBuilder(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().WriteTo.File("Logs\\log-.log", rollingInterval: RollingInterval.Day));
            startup = new Startup(builder.Configuration);
            builder.Configuration.AddJsonFile("configs/game/items.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile("configs/game/maps.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile("configs/game/application.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile("configs/game/servers.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile("configs/account.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile("configs/game/excludedItemsNewPlayer.json", optional: false, reloadOnChange: true);
            startup.ConfigureServices(builder.Services);
            return builder;
        }
    }
}
