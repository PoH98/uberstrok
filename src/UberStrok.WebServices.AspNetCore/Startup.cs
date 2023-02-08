using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SoapCore;
using System.IO;
using UberStrok.WebServices.AspNetCore.Core.Db.Tables;
using UberStrok.WebServices.AspNetCore.Core.Discord;
using UberStrok.WebServices.AspNetCore.Core.Manager;
using UberStrok.WebServices.AspNetCore.Helper;
using UberStrok.WebServices.AspNetCore.WebService;

namespace UberStrok.WebServices.AspNetCore
{
    public class Startup
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Startup));
        public IWebHostEnvironment HostingEnvironment { get; private set; }
        public IConfiguration Configuration { get; private set; }

        public static WebServiceConfiguration WebServiceConfiguration
        {
            get;
            private set;
        }
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            HostingEnvironment = env;
            Configuration = configuration;
            Log.Info("Initializing web services...");
            WebServiceConfiguration config = Utils.DeserializeJsonAt<WebServiceConfiguration>("assets/configs/main.json");
            if (config == null)
            {
                config = WebServiceConfiguration.Default;
                Utils.SerializeJsonAt("assets/configs/main.json", config);
            }
            WebServiceConfiguration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Register services.
            _ = services.AddSoapCore();

            // Register web services.
            _ = services.AddScoped<ApplicationWebService>();
            _ = services.AddScoped<AuthenticationWebService>();
            _ = services.AddScoped<ShopWebService>();
            _ = services.AddScoped<UserWebService>();
            _ = services.AddScoped<ClanWebService>();
            _ = services.AddScoped<PrivateMessageWebService>();
            _ = services.AddScoped<RelationshipWebService>();
            _ = services.AddScoped<ModerationWebService>();

            //Register Manager
            _ = services.AddSingleton<ClanManager>();
            _ = services.AddSingleton<UserManager>();
            _ = services.AddSingleton<ServerManager>();
            _ = services.AddSingleton<SecurityManager>();
            _ = services.AddSingleton<StreamManager>();
            _ = services.AddSingleton<ResourceManager>();
            _ = services.AddSingleton<GameSessionManager>();
            _ = services.AddSingleton<UberBeatManager>();

            //Register Tables
            _ = services.AddSingleton<ClanTable>();
            _ = services.AddSingleton<UserTable>();
            _ = services.AddSingleton<UberBeatTable>();
            _ = services.AddSingleton<SecurityTable>();
            _ = services.AddSingleton<StreamTable>();

            //Register Discord
            _ = services.AddSingleton<CoreDiscord>();
            _ = services.AddSingleton<UDPListener>();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                _ = app.UseDeveloperExceptionPage();
            }
            _ = app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "assets/images")),
                RequestPath = new PathString("/images")
            });
            _ = app.UseSoapEndpoint<AuthenticationWebService>("/2.0/AuthenticationWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            _ = app.UseSoapEndpoint<ApplicationWebService>("/2.0/ApplicationWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            _ = app.UseSoapEndpoint<ShopWebService>("/2.0/ShopWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            _ = app.UseSoapEndpoint<UserWebService>("/2.0/UserWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            _ = app.UseSoapEndpoint<ClanWebService>("/2.0/ClanWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            _ = app.UseSoapEndpoint<PrivateMessageWebService>("/2.0/PrivateMessageWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            _ = app.UseSoapEndpoint<RelationshipWebService>("/2.0/RelationshipWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
        }
    }
}
