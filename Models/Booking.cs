using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartEvent.Web.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        public int EventId { get; set; }

        public string UserId { get; set; }

        public int Quantity { get; set; }

        public DateTime BookingDate { get; set; }

        public string Status { get; set; }

        public Event Event { get; set; }

        public decimal TotalPrice { get; set; }

        public string? PaymentIntentId { get; set; }

        public string? PaymentStatus { get; set; }

        // ✅ FIXED PROPERTY
        public List<BookingTicket> BookingTickets { get; set; } = new List<BookingTicket>();
    }
}