using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusSeatBookingSystem.Entity.Entities
{
    public class enums
    {
        public enum SeatStatus
            {
                Available = 1,
                Booked = 2,
                Reserved = 3
        }

        public enum Gender
        {
            Male = 1,
            Female = 2,
            Other = 3
        }
    }
}
