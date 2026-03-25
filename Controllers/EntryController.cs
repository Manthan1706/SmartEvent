using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEvent.Web.Data;
using SmartEvent.Web.Models;
using System.Security.Claims;

namespace SmartEvent.Web.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class EntryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EntryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Validate([FromBody] QRRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.QRCode))
                    return Json(new { success = false, message = "QR Empty" });

                var value = request.QRCode.Trim();

                if (!value.StartsWith("TICKET|"))
                    return Json(new { success = false, message = "Invalid QR format" });

                var ticketId = int.Parse(value.Split('|')[1]);

                var ticket = _context.BookingTickets
                    .FirstOrDefault(x => x.BookingTicketId == ticketId);

                if (ticket == null)
                    return Json(new { success = false, message = "Ticket not found" });

                if (ticket.IsUsed)
                    return Json(new { success = false, message = "Ticket already used" });

                ticket.IsUsed = true;

                // get organizer user id
                var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                _context.EntryLogs.Add(new EntryLog
                {
                    BookingTicketId = ticket.BookingTicketId,
                    EntryTime = DateTime.Now,
                    StaffUserId = staffId
                });

                _context.SaveChanges();

                var booking = _context.Bookings.FirstOrDefault(x => x.BookingId == ticket.BookingId);
                var ev = _context.Events.FirstOrDefault(x => x.EventId == booking.EventId);

                return Json(new
                {
                    success = true,
                    eventName = ev.Title,
                    venue = ev.Venue,
                    date = ev.EventDate.ToString("dd MMM yyyy")
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }

    public class QRRequest
    {
        public string QRCode { get; set; }
    }
}