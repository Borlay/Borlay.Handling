using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling
{
    public class Roles : IRole
    {
        private readonly Dictionary<string, bool> roles = new Dictionary<string, bool>();

        public Roles(params string[] roles)
        {
            if (roles == null) return;

            foreach (var role in roles)
            {
                if (string.IsNullOrWhiteSpace(role))
                    throw new ArgumentNullException(nameof(role));

                this.roles.Add(role.ToLower(), true);
            }

        }

        public bool Contains(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentNullException(nameof(role));

            return roles.ContainsKey(role.ToLower());
        }
    }
}
