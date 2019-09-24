using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2CSample.Models
{
    public class SignInResponseContent
    {
        public string Email { get; set; }
        public string ValidCreds { get; set; }
        public string DisplayName { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }
}
