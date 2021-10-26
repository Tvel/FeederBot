using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace FeederBot;

static class Program
{
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

    static Task Main(string[] args)
    {
        return CreateHostBuilder(args).Build().RunAsync();
    }
}
