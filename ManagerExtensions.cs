using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NetSockets
{
    public static class ManagerExtensions
    {
        public static void AddWebSocketManager(this IServiceCollection services)
        {
            services.AddTransient<ConnectionManager>();

            var exportedTypes = Assembly.GetEntryAssembly()?.ExportedTypes;
            if (exportedTypes == null) return;
            foreach (var type in exportedTypes)
            {
                if (type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
                {
                    services.AddSingleton(type);
                }
            }
        }

        public static void MapWebSocketManager(this IApplicationBuilder app,
            PathString path,
            WebSocketHandler handler)
        {
            app.Map(path, (_app) => _app.UseMiddleware<ManagerMiddleware>(handler));
        }
    }
}