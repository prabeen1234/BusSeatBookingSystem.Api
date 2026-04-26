using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusSeatBookingSystem.Entity.Entities.BusEntities
{
    public class BusDetails
    {

        [Key]
        public Guid BusId { get; set; }
        [Required]
        public string BusName { get; set; }
        [Required]
        public string BusNumber { get; set; }
        [Required]
        public string BusType { get; set; }
        [Required]
        public string BusRoute { get; set; }
       
    }
}
