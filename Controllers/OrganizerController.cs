using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEvent.Web.Data;
using SmartEvent.Web.Models;

namespace SmartEvent.Web.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrganizerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }
    }
}