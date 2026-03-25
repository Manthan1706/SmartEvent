using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SmartEvent.Web.Data;
using SmartEvent.Web.Models;
using Stripe;
using Stripe.Checkout;

namespace SmartEvent.Web.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // BOOKINGS LIST
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingTickets)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return View(bookings);
        }

        // BOOKING PAGE
        public async Task<IActionResult> Create(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
                return NotFound();

            ViewBag.Event = ev;

            return View();
        }

        // CREATE STRIPE SESSION
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] PaymentRequest model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var ev = await _context.Events.FirstOrDefaultAsync(x => x.EventId == model.EventId);

            if (ev == null)
                return NotFound();



            if (model.Quantity < 1 || model.Quantity > 3)
                return BadRequest("Maximum 3 tickets allowed.");

            // ⭐ CHECK USER BOOKING LIMIT
            var alreadyBooked = await _context.Bookings
                .Where(b => b.UserId == userId && b.EventId == model.EventId)
                .SumAsync(b => (int?)b.Quantity) ?? 0;

            if (alreadyBooked + model.Quantity > 3)
                return BadRequest("You already booked maximum tickets for this event.");

            // ⭐ CHECK EVENT CAPACITY
            if (ev.Capacity < model.Quantity)
                return BadRequest("Not enough seats available.");



            var totalPrice = ev.Price * model.Quantity;

            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },

                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(ev.Price * 100),
                            Currency = "inr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = ev.Title
                            }
                        },
                        Quantity = model.Quantity
                    }
                },

                Mode = "payment",

                SuccessUrl = domain + "/Booking/PaymentSuccess?sessionId={CHECKOUT_SESSION_ID}",
                CancelUrl = domain + "/Booking/PaymentCancel",

                Metadata = new Dictionary<string, string>
                {
                    { "eventId", model.EventId.ToString() },
                    { "quantity", model.Quantity.ToString() },
                    { "userId", userId }
                }
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Json(new { url = session.Url });
        }

        // PAYMENT SUCCESS
        public async Task<IActionResult> PaymentSuccess(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return RedirectToAction("Index", "EventBrowse");

            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            if (session.PaymentStatus != "paid")
                return RedirectToAction("Index", "EventBrowse");

            int eventId = int.Parse(session.Metadata["eventId"]);
            int quantity = int.Parse(session.Metadata["quantity"]);
            string userId = session.Metadata["userId"];

            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
                return RedirectToAction("Index", "EventBrowse");

            // ⭐ CHECK USER LIMIT
            var alreadyBooked = await _context.Bookings
                .Where(b => b.UserId == userId && b.EventId == eventId)
                .SumAsync(b => (int?)b.Quantity) ?? 0;

            if (alreadyBooked + quantity > 3)
            {
                TempData["Error"] = "You already booked maximum tickets for this event.";
                return RedirectToAction("Index", "EventBrowse");
            }

            // CHECK CAPACITY
            if (ev.Capacity < quantity)
            {
                TempData["Error"] = "Event is sold out.";
                return RedirectToAction("Index", "EventBrowse");
            }

            // Reduce capacity
            ev.Capacity -= quantity;

            var booking = new Booking
            {
                EventId = eventId,
                UserId = userId,
                Quantity = quantity,
                TotalPrice = ev.Price * quantity,
                BookingDate = DateTime.Now,
                Status = "Confirmed",
                PaymentStatus = "Paid",
                PaymentIntentId = sessionId
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            GenerateTickets(booking.BookingId);

            return RedirectToAction("MyTickets");
        }

        // DELETE BOOKING
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int bookingId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                return NotFound();

            var tickets = await _context.BookingTickets
                .Where(t => t.BookingId == bookingId)
                .ToListAsync();

            _context.BookingTickets.RemoveRange(tickets);
            _context.Bookings.Remove(booking);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // CANCEL SINGLE TICKET
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTicket(int ticketId)
        {
            var ticket = await _context.BookingTickets
                .Include(t => t.Booking)
                .FirstOrDefaultAsync(t => t.BookingTicketId == ticketId);

            if (ticket == null)
                return NotFound();

            if (ticket.IsUsed)
            {
                TempData["Error"] = "Used ticket cannot be cancelled.";
                return RedirectToAction("MyTickets");
            }

            var booking = ticket.Booking;

            var ev = await _context.Events.FindAsync(booking.EventId);

            if (ev != null)
            {
                ev.Capacity += 1;
            }

            _context.BookingTickets.Remove(ticket);

            booking.Quantity -= 1;

            // UPDATE TOTAL PRICE AFTER CANCEL
            booking.TotalPrice = booking.Event.Price * booking.Quantity;

            if (booking.Quantity <= 0)
            {
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("MyTickets");
        }


        public IActionResult PaymentCancel(string sessionId)
        {
            var booking = _context.Bookings
                .FirstOrDefault(b => b.PaymentIntentId == sessionId);

            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                _context.SaveChanges();
            }

            TempData["Error"] = "Payment Cancelled";

            return RedirectToAction("Index", "EventBrowse");
        }



      



        // GENERATE TICKETS
        private void GenerateTickets(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null) return;

            for (int i = 0; i < booking.Quantity; i++)
            {
                var ticket = new BookingTicket
                {
                    BookingId = booking.BookingId,
                    //QRCodeValue = $"TICKET-{Guid.NewGuid()}",
                    QRCodeValue = "",
                    IsUsed = false
                };

                _context.BookingTickets.Add(ticket);
                _context.SaveChanges();

                ticket.QRCodeValue = $"TICKET|{ticket.BookingTicketId}";
            }

            _context.SaveChanges();
        }


        // QR GENERATOR
        private string GenerateQRCode(string qrText)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            Base64QRCode qrCode = new Base64QRCode(qrCodeData);

            return qrCode.GetGraphic(20);
        }

        // MY TICKETS
        public IActionResult MyTickets()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tickets = _context.BookingTickets
                .Include(t => t.Booking)
                .ThenInclude(b => b.Event)
                .Where(t => t.Booking.UserId == userId)
                .ToList();

            var qrCodes = new Dictionary<int, string>();

            foreach (var ticket in tickets)
            {
                qrCodes[ticket.BookingTicketId] =
                    GenerateQRCode(ticket.QRCodeValue ?? "");
            }

            ViewBag.QRCodes = qrCodes;

            return View(tickets);
        }
    }
}













//using System.Security.Claims;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using QRCoder;
//using SmartEvent.Web.Data;
//using SmartEvent.Web.Models;
//using Stripe;
//using Stripe.Checkout;

//namespace SmartEvent.Web.Controllers
//{
//    [Authorize]
//    public class BookingController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public BookingController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        // BOOKINGS LIST
//        public async Task<IActionResult> Index()
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//            var bookings = await _context.Bookings
//                .Include(b => b.Event)
//                .Include(b => b.BookingTickets)
//                .Where(b => b.UserId == userId)
//                .ToListAsync();

//            return View(bookings);
//        }

//        // BOOKING PAGE
//        public async Task<IActionResult> Create(int eventId)
//        {
//            var ev = await _context.Events.FindAsync(eventId);

//            if (ev == null)
//                return NotFound();

//            ViewBag.Event = ev;

//            return View();
//        }

//        // CREATE BOOKING
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(int eventId, int quantity)
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//            var ev = await _context.Events.FirstOrDefaultAsync(x => x.EventId == eventId);

//            if (ev == null)
//                return NotFound();

//            // USER LIMIT (MAX 3)
//            var alreadyBooked = await _context.Bookings
//                .Where(b => b.UserId == userId && b.EventId == eventId)
//                .SumAsync(b => (int?)b.Quantity) ?? 0;

//            if (alreadyBooked + quantity > 3)
//            {
//                TempData["Error"] = "You can only book maximum 3 tickets for this event.";
//                return RedirectToAction("Index", "EventBrowse");
//            }

//            // CHECK CAPACITY
//            if (ev.Capacity < quantity)
//            {
//                TempData["Error"] = "Not enough seats available.";
//                return RedirectToAction("Index", "EventBrowse");
//            }

//            // REDUCE CAPACITY
//            ev.Capacity -= quantity;

//            // ⭐ CALCULATE TOTAL PRICE
//            var totalPrice = ev.Price * quantity;

//            var booking = new Booking
//            {
//                EventId = eventId,
//                UserId = userId,
//                Quantity = quantity,
//                TotalPrice = totalPrice,
//                BookingDate = DateTime.Now,
//                Status = "Confirmed"
//            };

//            _context.Bookings.Add(booking);
//            await _context.SaveChangesAsync();

//            // CREATE TICKETS
//            for (int i = 0; i < quantity; i++)
//            {
//                var ticket = new BookingTicket
//                {
//                    BookingId = booking.BookingId,
//                    QRCodeValue = Guid.NewGuid().ToString(),
//                    IsUsed = false
//                };

//                _context.BookingTickets.Add(ticket);
//            }

//            await _context.SaveChangesAsync();

//            // FORMAT QR CODE VALUE
//            var tickets = await _context.BookingTickets
//                .Where(t => t.BookingId == booking.BookingId)
//                .ToListAsync();

//            foreach (var ticket in tickets)
//            {
//                ticket.QRCodeValue = $"TICKET|{ticket.BookingTicketId}";
//            }

//            await _context.SaveChangesAsync();

//            return RedirectToAction("MyTickets");
//        }

//        // CANCEL FULL BOOKING
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Cancel(int bookingId)
//        {
//            var booking = await _context.Bookings
//                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

//            if (booking == null)
//                return NotFound();

//            var tickets = await _context.BookingTickets
//                .Where(t => t.BookingId == bookingId)
//                .ToListAsync();

//            if (tickets.Any(t => t.IsUsed))
//            {
//                TempData["Error"] = "Used tickets cannot be cancelled.";
//                return RedirectToAction("Index");
//            }

//            var ev = await _context.Events.FindAsync(booking.EventId);

//            if (ev != null)
//            {
//                ev.Capacity += booking.Quantity;
//            }

//            _context.BookingTickets.RemoveRange(tickets);
//            _context.Bookings.Remove(booking);

//            await _context.SaveChangesAsync();

//            return RedirectToAction("Index");
//        }

//        // DELETE BOOKING
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Delete(int bookingId)
//        {
//            var booking = await _context.Bookings
//                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

//            if (booking == null)
//                return NotFound();

//            var tickets = await _context.BookingTickets
//                .Where(t => t.BookingId == bookingId)
//                .ToListAsync();

//            _context.BookingTickets.RemoveRange(tickets);
//            _context.Bookings.Remove(booking);

//            await _context.SaveChangesAsync();

//            return RedirectToAction("Index");
//        }

//        // CANCEL SINGLE TICKET
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> CancelTicket(int ticketId)
//        {
//            var ticket = await _context.BookingTickets
//                .Include(t => t.Booking)
//                .FirstOrDefaultAsync(t => t.BookingTicketId == ticketId);

//            if (ticket == null)
//                return NotFound();

//            if (ticket.IsUsed)
//            {
//                TempData["Error"] = "Used ticket cannot be cancelled.";
//                return RedirectToAction("MyTickets");
//            }

//            var booking = ticket.Booking;

//            var ev = await _context.Events.FindAsync(booking.EventId);

//            if (ev != null)
//            {
//                ev.Capacity += 1;
//            }

//            _context.BookingTickets.Remove(ticket);

//            booking.Quantity -= 1;

//            // UPDATE TOTAL PRICE AFTER CANCEL
//            booking.TotalPrice = booking.Event.Price * booking.Quantity;

//            if (booking.Quantity <= 0)
//            {
//                _context.Bookings.Remove(booking);
//            }

//            await _context.SaveChangesAsync();

//            return RedirectToAction("MyTickets");
//        }




//        // QR GENERATOR
//        private string GenerateQRCode(string qrText)
//        {
//            QRCodeGenerator qrGenerator = new QRCodeGenerator();
//            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
//            Base64QRCode qrCode = new Base64QRCode(qrCodeData);

//            return qrCode.GetGraphic(20);
//        }

//        // MY TICKETS PAGE
//        public IActionResult MyTickets()
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//            var tickets = _context.BookingTickets
//                .Include(t => t.Booking)
//                .ThenInclude(b => b.Event)
//                .Where(t => t.Booking.UserId == userId)
//                .ToList();

//            var qrCodes = new Dictionary<int, string>();

//            foreach (var ticket in tickets)
//            {
//                qrCodes[ticket.BookingTicketId] =
//                    GenerateQRCode(ticket.QRCodeValue ?? "");
//            }

//            ViewBag.QRCodes = qrCodes;

//            return View(tickets);
//        }


//        [HttpPost]
//        public async Task<IActionResult> CreateCheckoutSession([FromBody] PaymentRequest model)
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//            var ev = await _context.Events.FirstOrDefaultAsync(x => x.EventId == model.EventId);

//            if (ev == null)
//                return NotFound();

//            if (model.Quantity > 3)
//            {
//                return BadRequest("Maximum 3 tickets allowed.");
//            }

//            var totalPrice = ev.Price * model.Quantity;

//            var booking = new Booking
//            {
//                EventId = model.EventId,
//                UserId = userId,
//                Quantity = model.Quantity,
//                TotalPrice = totalPrice,
//                BookingDate = DateTime.Now,
//                Status = "Pending",
//                PaymentStatus = "Pending"
//            };

//            //_context.Bookings.Add(booking);
//            //await _context.SaveChangesAsync();

//            var domain = $"{Request.Scheme}://{Request.Host}";

//            var options = new SessionCreateOptions
//            {
//                PaymentMethodTypes = new List<string> { "card" },

//                LineItems = new List<SessionLineItemOptions>
//        {
//            new SessionLineItemOptions
//            {
//                PriceData = new SessionLineItemPriceDataOptions
//                {
//                    UnitAmount = (long)(totalPrice * 100),
//                    Currency = "inr",
//                    ProductData = new SessionLineItemPriceDataProductDataOptions
//                    {
//                        Name = ev.Title
//                    }
//                },
//                Quantity = 1
//            }
//        },

//                Mode = "payment",

//                SuccessUrl = domain + "/Booking/PaymentSuccess?sessionId={CHECKOUT_SESSION_ID}",
//                CancelUrl = domain + "/Booking/PaymentCancel"
//            };

//            var service = new SessionService();
//            Session session = service.Create(options);

//            booking.PaymentIntentId = session.Id;

//            await _context.SaveChangesAsync();

//            return Json(new { url = session.Url });
//        }



//        private void GenerateTickets(int bookingId)
//        {
//            var booking = _context.Bookings
//                .Include(b => b.Event)
//                .FirstOrDefault(b => b.BookingId == bookingId);

//            if (booking == null) return;

//            for (int i = 0; i < booking.Quantity; i++)
//            {
//                var ticket = new BookingTicket
//                {
//                    BookingId = booking.BookingId,
//                    QRCodeValue = $"TICKET-{Guid.NewGuid()}",
//                    IsUsed = false
//                };

//                _context.BookingTickets.Add(ticket);
//            }

//            _context.SaveChanges();
//        }




//        public IActionResult PaymentSuccess(int bookingId)
//        {
//            var booking = _context.Bookings
//                .Include(b => b.Event)
//                .FirstOrDefault(b => b.BookingId == bookingId);

//            if (booking == null)
//                return NotFound();

//            booking.PaymentStatus = "Paid";
//            booking.Status = "Confirmed";

//            _context.SaveChanges();

//            // Generate tickets AFTER payment
//            GenerateTickets(bookingId);

//            return RedirectToAction("MyTickets");
//        }

//        public IActionResult PaymentCancel(string sessionId)
//        {
//            var booking = _context.Bookings
//                .FirstOrDefault(b => b.PaymentIntentId == sessionId);

//            if (booking != null)
//            {
//                _context.Bookings.Remove(booking);
//                _context.SaveChanges();
//            }

//            TempData["Error"] = "Payment Cancelled";

//            return RedirectToAction("Index", "EventBrowse");
//        }

//    }
//}