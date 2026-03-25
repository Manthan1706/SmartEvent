using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvent.Web.Data;
using SmartEvent.Web.Models;

namespace SmartEvent.Web.Controllers
{
    public class EventBrowseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventBrowseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // SHOW ONLY ACTIVE EVENTS FOR USERS
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Where(e => e.Status == "Active" && e.EventDate >= DateTime.Now)
                .ToListAsync();

            return View(events);
        }

        // EVENT DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
                return NotFound();

            return View(ev);
        }
    }
}