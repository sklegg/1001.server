namespace Server1001.Shared.Authorization;

using Server1001.Services;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IUserService userService, IJwtUtils jwtUtils)
    {
        var cookie = context.Request.Cookies["1001"] ?? "";
        var userId = jwtUtils.ValidateToken(cookie);
        if (userId != null)
        {
            // attach user to context on successful jwt validation
            context.Items["User"] = await userService.GetUserByIdAsync(userId.Value);
        }

        await _next(context);
    }
}