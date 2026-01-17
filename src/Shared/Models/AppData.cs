using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class AppData
    {
        public int Id { get; set; }
        public string Key { get; set; } = "Default";
        public string Value { get; set; } = "Initial Value";
        public DateTime UpdatedAt { get; set; }
    }
}
