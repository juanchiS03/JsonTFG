using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Json.Controllers
{
    public class UmlController : Controller
    {
        public IActionResult VistaGraf()
        {
            var jsonData = TempData["jsonData"] as string;
            if (string.IsNullOrEmpty(jsonData))
            {
                return BadRequest("No data available.");
            }

            ViewBag.JsonData = jsonData;
            return View();
        }
    }
}
