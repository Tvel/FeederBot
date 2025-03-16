using System;
using Discord.WebSocket;
using FeederBot.Discord;
using FeederBot.Jobs;
using FeederBot.Jobs.Storage;
using FeederBot.System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FeederBot;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(Configuration);
            builder.AddConsole();
        });
        services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

        string? oltpEndpoint = Configuration["OTLP:Endpoint"];
        if (oltpEndpoint != null)
        {
            services.AddOpenTelemetry()
                .WithTracing((providerBuilder) => providerBuilder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    //.AddSource("")
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault().AddService(
                            $"FeederBot:{this.Configuration["ASPNETCORE_ENVIRONMENT"]}"))
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(oltpEndpoint);
                    }));
        }

        services.Configure<DatabaseConfig>(Configuration.GetSection("SQLite"));
        services.Configure<AuthSettings>(Configuration.GetSection("Auth"));
        services.Configure<DiscordSettings>(Configuration.GetSection("Discord"));
        services.Configure<FeederSettings>(Configuration.GetSection("Feeder"));

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<JobSchedulesStorage>();
        services.AddSingleton<JobRunner>();
        services.AddHostedService(static s => s.GetService<JobRunner>());

        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton<SingleChannelDiscordSender>();
        services.AddSingleton<IMessageReceiver>(static s => s.GetService<SingleChannelDiscordSender>() ?? throw new NullReferenceException("Invalid Receiver"));
        services.AddHostedService(static s => s.GetService<SingleChannelDiscordSender>());

        //services.AddSingleton<IMessageReceiver, LogMessageReceiver>();

        services.AddSingleton<IJobStorage, JobDatabaseStorage>();
        services.AddSingleton<IJobApiStorage, JobDatabaseStorage>();

        services.AddSingleton<IDatabaseBootstrap, DatabaseBootstrap>();

        services.AddControllers();
        services.AddSwaggerGen(
            options =>
                {
                    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "basic",
                        In = ParameterLocation.Header,
                        Description = "Basic Authorization header using the Bearer scheme."
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                                                       {
                                                               {
                                                                   new OpenApiSecurityScheme
                                                                       {
                                                                           Reference = new OpenApiReference
                                                                               {
                                                                                   Type = ReferenceType.SecurityScheme,
                                                                                   Id = "basic"
                                                                               }
                                                                       },
                                                                   new string[] {}
                                                               }
                                                       });
                });
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", options => { });
        services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("BasicAuthentication").RequireAuthenticatedUser().Build();
                options.AddPolicy("BasicAuthentication", new AuthorizationPolicyBuilder("BasicAuthentication").RequireAuthenticatedUser().Build());
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

        app.UseRouting();

        app.UseAuthorization();
        app.UseEndpoints(
            e =>
                {
                    e.MapControllers();
                });
        //
        // app.UseEndpoints(
        //     endpoints =>
        //         {
        //             endpoints.MapGrpcService<GreeterService>();
        //
        //             endpoints.MapGet(
        //                 "/",
        //                 async context =>
        //                     {
        //                         await context.Response.WriteAsync(
        //                             "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        //                     });
        //         });

        serviceProvider.GetService<IDatabaseBootstrap>()?.Setup();
    }
}
