using System.ComponentModel.DataAnnotations;

namespace SmartEvent.Web.Models
{
    public class EntryLog
    {
        [Key]
        public int EntryLogId { get; set; }

        public int BookingTicketId { get; set; }

        public DateTime EntryTime { get; set; } = DateTime.Now;

        public string StaffUserId { get; set; }

        public BookingTicket BookingTicket { get; set; }
    }
}