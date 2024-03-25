using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendMany.Model
{
    internal class DestinationAdress
    {
        public DestinationAdress(string id, long amount)
        {
            Id = id;
            Amount = amount;
        }

        public string Id { get; set; }
        public long Amount { get; set; }
    }
}
