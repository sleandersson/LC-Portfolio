using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC_Portfolio.Database
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Age { get; set; }
        public string Gender { get; set; }

        // You might need a property to combine FirstName and LastName if you want to display them as a single "Name" column.
        public string Name => $"{FirstName} {LastName}";
    }


}
