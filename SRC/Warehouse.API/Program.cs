
namespace Warehouse.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            using WebApplication app = builder.Build();

            app
                .UseHttpsRedirection()
                .UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
