using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CabinReservation.Persistence.Domain
{
    public class ReservationPolicy
    {
        [Key]
        public int PolicyId { get; set; }
        public int Name { get; set; }
        public string Value { get; set; }

        [Key]
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }

    }
}
