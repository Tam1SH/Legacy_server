using Microsoft.AspNetCore.Hosting;

namespace SasavnServer
{
	public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", false, true);
                    config.AddJsonFile($"appsettings.json", false, true);
                })
				
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
						.ConfigureKestrel(options => {

							options.ListenAnyIP(1448, o => {
								o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
							});

							options.ListenAnyIP(80, listenOptions =>
							{
								listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
							});
						});
                });
    }
}
