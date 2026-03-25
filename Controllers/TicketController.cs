using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvent.Web.Data;
using SmartEvent.Web.Models;

namespace SmartEvent.Web.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tickets = _context.Tickets.Include(t => t.Event);
            return View(await tickets.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.Events = _context.Events.ToList();
            return View();
        }

       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket ticket)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Events = _context.Events.ToList();
                return View(ticket);
            }

            // Automatically set available quantity
            ticket.AvailableQuantity = ticket.Quantity;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}