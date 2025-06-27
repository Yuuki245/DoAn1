using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class RoleMiddleware
{
    private readonly RequestDelegate _next;

    public RoleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity.IsAuthenticated && context.User.IsInRole("Administrator"))
        {
            Console.WriteLine($"User {context.User.Identity.Name} is an Administrator.");
        }
        else if (context.User.Identity.IsAuthenticated)
        {
            Console.WriteLine($"User {context.User.Identity.Name} is NOT an Administrator.");
        }

        await _next(context);
    }

}
