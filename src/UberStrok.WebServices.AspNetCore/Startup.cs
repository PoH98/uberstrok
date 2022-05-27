using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SoapCore;
using UberStrok.Core;
using UberStrok.WebServices.AspNetCore.Authentication;
using UberStrok.WebServices.AspNetCore.Authentication.Jwt;
using UberStrok.WebServices.AspNetCore.Configurations;
using UberStrok.WebServices.AspNetCore.Database;
using UberStrok.WebServices.AspNetCore.Database.LiteDb;

namespace UberStrok.WebServices.AspNetCore
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
            => Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            // Register configurations.

            services.Configure<MapsConfiguration>(Configuration.GetSection("Maps"));
            services.Configure<ItemsConfiguration>(Configuration.GetSection("Items"));
            services.Configure<ServersConfiguration>(Configuration.GetSection("Servers"));
            services.Configure<ApplicationConfiguration>(Configuration.GetSection("Application"));
            services.Configure<AccountConfiguration>(Configuration.GetSection("Account"));
            services.Configure<AuthConfiguration>(Configuration.GetSection("Auth"));

            // Register services.
            services.AddSingleton<IDbService, LiteDbService>();
            services.AddSingleton<IAuthService, JwtAuthService>();
            services.AddSingleton<ISessionService, SessionService>();

            services.AddSoapCore();

            services.AddSingleton(s => new ItemManager(s.GetRequiredService<IOptions<ItemsConfiguration>>().Value));
            services.AddSingleton(s => new MapManager(s.GetRequiredService<IOptions<MapsConfiguration>>().Value));

            // Register web services.
            services.AddSingleton<ApplicationWebService>();
            services.AddSingleton<AuthenticationWebService>();
            services.AddSingleton<ShopWebService>();
            services.AddSingleton<UserWebService>();
            // services.AddSingleton<ClanWebService>();
            services.AddSingleton<PrivateMessageWebService>();
            services.AddSingleton<RelationshipWebService>();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();
            app.UseSoapEndpoint<AuthenticationWebService>("/AuthenticationWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<ApplicationWebService>("/ApplicationWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<ShopWebService>("/ShopWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<UserWebService>("/UserWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            // app.UseSoapEndpoint<ClanWebService>("ClanWebService", new BasicHttpBinding());
            app.UseSoapEndpoint<PrivateMessageWebService>("/PrivateMessageWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            app.UseSoapEndpoint<RelationshipWebService>("/RelationshipWebService", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
        }
    }
}
