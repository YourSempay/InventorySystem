using System.Net;
using static API.Services.Exceptions;

namespace API.Services
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ExceptionMiddleware> logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (ValidationException ex) // 400 — ошибки валидации
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex) // 401 — неавторизован
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (ForbiddenException ex) // 403 — нет прав
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex) // 404 — не найдено
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (ConflictException ex) // 409 — конфликт
            {
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) // 422 — бизнес-ошибка
            {
                context.Response.StatusCode = 422;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (Exception ex) // 500 — внутренняя ошибка
            {
                logger.LogError(ex, "Unhandled error");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера" });
            }
        }
    }
}