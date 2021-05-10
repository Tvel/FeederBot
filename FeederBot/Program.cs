using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace FeederBot
{
    static class Program
    {
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                    {
                        Startup startup = new Startup();
                        startup.ConfigureServices(services);
                    });
        
        static Task Main(string[] args)
        {
            return CreateHostBuilder(args).Build().RunAsync();
        }
    }
}
