using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvent.Web.Data;
using SmartEvent.Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SmartEvent.Web.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LIST EVENTS
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.ToListAsync();
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

        // CREATE PAGE
        public IActionResult Create()
        {
            return View();
        }

        // CREATE EVENT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model)
        {
            model.OrganizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.Status = "Pending";

            if (!ModelState.IsValid)
                return View(model);

            _context.Events.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // EDIT PAGE
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event model)
        {
            if (id != model.EventId)
                return NotFound();

            var existingEvent = await _context.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (existingEvent == null)
                return NotFound();

            // Preserve required fields
            model.OrganizerId = existingEvent.OrganizerId;
            model.Status = existingEvent.Status;

            if (!ModelState.IsValid)
                return View(model);

            _context.Update(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // DELETE PAGE
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // CONFIRM DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev != null)
            {
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}