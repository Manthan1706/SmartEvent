using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartEvent.Web.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }

        public int AvailableQuantity { get; set; }

        [Required]
        public int EventId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public Event? Event { get; set; }
    }
}