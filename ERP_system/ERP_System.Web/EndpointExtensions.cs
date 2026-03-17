using ERP_System.Web.appMaps;

namespace ERP_System.Web
{
    public static class EndpointExtensions
    {
        public static void MapAllEndpoints(this WebApplication app)
        {
            var endpointTypes = typeof(Program).Assembly.GetTypes()
                .Where(t => typeof(IEndpoint).IsAssignableFrom(t)
                && !t.IsInterface && !t.IsAbstract);

            foreach(var type in endpointTypes)
            {
                var instance = Activator.CreateInstance(type) as IEndpoint;

                instance?.Map(app);
            }
        }
    }
}