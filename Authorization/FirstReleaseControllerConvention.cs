using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace OpenQMS.Authorization;

public class FirstReleaseControllerConvention : IApplicationModelConvention
{
    private static readonly HashSet<string> DeferredControllers = new(StringComparer.Ordinal)
    {
        "AppDocuments",
        "Assets",
        "Capas",
        "Changes",
        "Deviations",
        "Materials",
        "Processes",
        "Products",
        "Trainings"
    };

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers
            .Where(controller => DeferredControllers.Contains(controller.ControllerName))
            .ToArray())
        {
            application.Controllers.Remove(controller);
        }
    }
}
