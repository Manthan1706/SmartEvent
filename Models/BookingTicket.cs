namespace SmartEvent.Web.Models
{
    public class BookingTicket
    {
        public int BookingTicketId { get; set; }

        public int BookingId { get; set; }

        public string QRCodeValue { get; set; }

        public bool IsUsed { get; set; }

        public Booking Booking { get; set; }
    }
}