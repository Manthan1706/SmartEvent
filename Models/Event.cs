using System.ComponentModel.DataAnnotations;

namespace SmartEvent.Web.Models
{
    public class Event
    {
        public int EventId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public string Venue { get; set; }

        [Required]
        public int Capacity { get; set; }

        // ADD THIS
        [Required]
        public decimal Price { get; set; }

        public string? Status { get; set; }

        public string? OrganizerId { get; set; }

        public ICollection<Ticket>? Tickets { get; set; }
    }
}