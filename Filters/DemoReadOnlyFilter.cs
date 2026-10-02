using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PharmaSkincare.Filters
{
    // Users in the "Demo" role can view every page but cannot change anything:
    // all POST/PUT/DELETE requests are blocked (except logging out).
    public class DemoReadOnlyFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            if (!http.User.IsInRole("Demo")) return;
            if (HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method)) return;

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();
            if (controller == "Account" && action == "Logout") return;

            context.Result = new ContentResult
            {
                StatusCode = StatusCodes.Status403Forbidden,
                ContentType = "text/html",
                Content = "<p style='font-family:sans-serif;padding:2rem'>" +
                          "Changes are disabled for the demo admin account. " +
                          "<a href='javascript:history.back()'>Go back</a></p>"
            };
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}