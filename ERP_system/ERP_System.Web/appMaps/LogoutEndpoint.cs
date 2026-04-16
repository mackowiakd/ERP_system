using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace ERP_System.Web.appMaps
{
    public class LogoutEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/logout", (HttpContext context) =>
            {
                context.Response.Cookies.Delete("logged_user");
                return Results.Redirect("/index.html");
            });
        }
    }
}
