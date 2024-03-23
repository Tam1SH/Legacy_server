
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using WebApplication2;
using Microsoft.AspNetCore.Http.Connections;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using SiteAPI;
using SiteAPI.Hubs;
using SasavnServer.ApiClasses;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Microsoft.AspNetCore.Diagnostics;
using SasavnServer.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
using SasavnServer.Controllers.Users;
using SasavnServer.Controllers.Authenticate;
using SasavnServer.Controllers.ChangeLogs;
using SasavnServer.Controllers.Assets;
using SasavnServer.Controllers.Admin;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using SasavnServer.Service;

namespace SasavnServer
{
	public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddControllers();
            services.AddMemoryCache();
			services.AddEndpointsApiExplorer();
			services.AddGrpc();

			services.AddOpenApiDocument();
			services.AddSwaggerGen();

            services.AddDbContext<DataBaseContext>(options =>
            {
                using var connection = new MySqlConnection(Configuration["connectiondb"]);
                connection.Open();
                options.UseMySql(Configuration["connectiondb"], new MySqlServerVersion(connection.ServerVersion));

            }, ServiceLifetime.Scoped);

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(new string[]
                    {
                        JwtBearerDefaults.AuthenticationScheme
                    }
                    )
                    .RequireAuthenticatedUser()
                    .Build();
				
            });

			
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = AuthOptions.ISSUER,
                        ValidateAudience = true,
                        ValidAudience = AuthOptions.AUDIENCE,
                        ValidateLifetime = false,
                        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                        ValidateIssuerSigningKey = true,
                    };
                })
                .AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = Configuration["GoogleClientId"];
                    googleOptions.ClientSecret = Configuration["GoogleSecret"];
                    googleOptions.CallbackPath = new PathString("/api/authenticate/google-response");
                    
                })
                .AddSteam(options =>
                {
                    options.ApplicationKey = Configuration["SteamSecret"];
                    options.CallbackPath = new PathString("/api/authenticate/steamResponse");
                })
                .AddVkontakte(options =>
                {
                    options.ClientId = Configuration["VkId"];
                    options.ClientSecret = Configuration["VkSecret"];
                    options.CallbackPath = new PathString("/api/authenticate/vkResponse");
                })
                .AddDiscord(options =>
                {
                    options.ClientId = Configuration["discordId"];
                    options.ClientSecret = Configuration["discordSecret"];
                    options.CallbackPath = new PathString("/api/authenticate/discordResponse/");
                })
                .AddCookie(options =>
                {
                    options.LoginPath = new PathString("/Login");
                    options.LogoutPath = new PathString("/Profile/AccountManagement");
                    
                });

            services.AddLogging(conf =>
            {
                conf.AddSerilog();
                conf.AddFile($"Logs/logs-{DateTime.Now:yyyy-MM-dd}.txt");
            });

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Fatal)
                .WriteTo.File($"Logs/my-logs-{DateTime.Now:yyyy_MM_dd}.txt")
                .CreateLogger();


            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IAssetsStore, AssetsStore>();
            services.AddScoped<ChangeLogsService>();
            services.AddScoped<AssetsService>();
            services.AddScoped<DiscordSocketClient>();
			services.AddScoped<FileSystemService>();
            services.AddScoped<UsersService>();
            services.AddScoped<AdminService>();
            services.AddScoped<AuthenticateService>();
			services.AddScoped(typeof(LoggerService<>));
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<ILogger, Logger<ILogger>>();
            services.AddSingleton<LauncherHub>();
            services.AddSingleton<Timers>();
            services.AddSingleton<Usefull>();
        }


		class GlobalExceptionHandler { }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
			app.UseOpenApi();
			
            
            //Need to create singletons
            app.ApplicationServices.GetService<Timers>();
            app.ApplicationServices.GetService<Usefull>();

			var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ru"),
				new CultureInfo("zh"),
            };
			
			app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
				RequestCultureProviders = new List<IRequestCultureProvider> {
					new CookieRequestCultureProvider {
						CookieName = "lang",
					},
					new AcceptLanguageHeaderRequestCultureProvider()
				}
            });


            //app.UseCookiePolicy(new CookiePolicyOptions
            //{
            //    HttpOnly = HttpOnlyPolicy.Always,
            //    Secure = CookieSecurePolicy.Always,
            //    MinimumSameSitePolicy = SameSiteMode.Strict,
            //    
            //});


            app.UseExceptionHandler(e =>
            {	

                e.Run(async context =>
                {
					using var scope = app.ApplicationServices.CreateScope();
                	var logger = scope.ServiceProvider.GetService<LoggerService<GlobalExceptionHandler>>();
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
					var user = context.User.GetUserData();

					logger?.Error(new LogParams {
						Title = "Unexcepted exception",
						Message = exceptionHandlerPathFeature?.Error,
						UserId = user?.Id ?? -1
					});

                    if (env.IsDevelopment() && exceptionHandlerPathFeature != null)
                        await context.Response.WriteAsJsonAsync(new ErrorCode(-1, exceptionHandlerPathFeature.Error.Message));
                    else 
                        await context.Response.WriteAsJsonAsync(new ErrorCode(-1, "unknown"));
                });
            });

            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                RequireHeaderSymmetry = false
            };

            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardedHeadersOptions);

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".yml"] = "text/html";
            provider.Mappings[".sas"] = "text/html";
            provider.Mappings[".lua"] = "text/html";
            provider.Mappings[".ytd"] = "application/octet-stream";


            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            //app.UseCors("CorsPolicy");
            app.UseCorsMiddleware();
		
            app.UseEndpoints(endpoints => {


                endpoints.MapControllers();
				endpoints.MapGrpcService<GChangelogsController>();

                endpoints.MapHub<WorkshopHub>("api/hub/workshop", options =>
                {
                    options.Transports =
                        HttpTransportType.WebSockets;
                });

                endpoints.MapHub<LauncherHub>("api/hub/launcher", options =>
                {
                    options.Transports =
                        HttpTransportType.WebSockets;
                });
            });


            app.UseWebSockets();

        }
    }
}
