using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Model
{
    public class UserRegisterOutputModel
    {
        public bool Registered { get; set; }
        public string Username { get; set; }
        public string EmailAddress { get; set; }
        public string TokenEndPoint { get; set; }
    }
}
