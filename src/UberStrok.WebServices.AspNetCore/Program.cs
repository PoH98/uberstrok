using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UberStrok.WebServices.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(delegate (IWebHostBuilder webBuilder)
            {
                _ = webBuilder.UseStartup<Startup>().UseUrls("http://0.0.0.0:5000").UseKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = 256000;
                });
            }).ConfigureLogging(delegate (HostBuilderContext hostingContext, ILoggingBuilder logging)
            {
                _ = logging.AddLog4Net();
                _ = logging.SetMinimumLevel(LogLevel.Debug);
            });
        }
    }
}
