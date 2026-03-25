using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvent.Web.Data;
using SmartEvent.Web.Models;

namespace SmartEvent.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalEvents = await _context.Events.CountAsync();
            ViewBag.TotalTickets = await _context.Tickets.CountAsync();

            var events = await _context.Events.ToListAsync();

            return View(events);
        }

        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
                return NotFound();

            ev.Status = status;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> PendingEvents()
        {
            var events = await _context.Events
                .Where(e => e.Status == "Pending")
                .ToListAsync();

            return View(events);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
                return NotFound();

            var bookings = await _context.Bookings
                .Where(b => b.EventId == id)
                .ToListAsync();

            if (bookings.Any())
            {
                var bookingIds = bookings.Select(b => b.BookingId).ToList();

                var tickets = await _context.BookingTickets
                    .Where(t => bookingIds.Contains(t.BookingId))
                    .ToListAsync();

                _context.BookingTickets.RemoveRange(tickets);
                _context.Bookings.RemoveRange(bookings);
            }

            _context.Events.Remove(ev);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}