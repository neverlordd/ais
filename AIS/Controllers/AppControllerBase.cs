using Microsoft.AspNetCore.Mvc;

namespace AIS.Controllers;

public abstract class AppControllerBase : Controller
{
    protected void SetStatus(string type, string message)
    {
        TempData["StatusType"] = type;
        TempData["StatusMessage"] = message;
    }
}
