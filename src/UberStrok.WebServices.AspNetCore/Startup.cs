using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SoapCore;
using System;
using System.IO;
using UberStrok.WebServices.AspNetCore.Core.Discord;
using UberStrok.WebServices.AspNetCore.Core.Manager;
using UberStrok.WebServices.AspNetCore.Core.Session;
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
            Log.Info("Initializing udp listener");
            UDPListener.Initialise();
            Log.Info("Initializing discord bot");
            CoreDiscord.Initialise();
            Log.Info("Initializing web services...");
            WebServiceConfiguration config = Utils.DeserializeJsonAt<WebServiceConfiguration>("assets/configs/main.json");
            if (config == null)
            {
                config = WebServiceConfiguration.Default;
                Utils.SerializeJsonAt("assets/configs/main.json", config);
            }
            WebServiceConfiguration = config;
            try
            {
                UserManager.Init();
                ClanManager.Init();
                StreamManager.Init();
                ServerManager.Init();
                ResourceManager.Init();
                GameSessionManager.Init();
                SecurityManager.Init();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex);
                Log.Fatal("Unable to initialize web services.");
                throw;
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Register services.
            services.AddSoapCore();

            // Register web services.
            services.AddScoped<ApplicationWebService>();
            services.AddScoped<AuthenticationWebService>();
            services.AddScoped<ShopWebService>();
            services.AddScoped<UserWebService>();
            services.AddScoped<ClanWebService>();
            services.AddScoped<PrivateMessageWebService>();
            services.AddScoped<RelationshipWebService>();
            services.AddScoped<ModerationWebService>();
            services.AddMemoryCache();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "assets/images")),
                RequestPath = new PathString("/images")
            });
            app.UseSoapEndpoint<AuthenticationWebService>("/2.0/AuthenticationWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<ApplicationWebService>("/2.0/ApplicationWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<ShopWebService>("/2.0/ShopWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<UserWebService>("/2.0/UserWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<ClanWebService>("/2.0/ClanWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<PrivateMessageWebService>("/2.0/PrivateMessageWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<RelationshipWebService>("/2.0/RelationshipWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
        }
    }
}
