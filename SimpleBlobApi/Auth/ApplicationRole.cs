using Microsoft.AspNetCore.Identity;
using System;

namespace SimpleBlobApi.Auth
{
    /// <summary>
    /// Application role.
    /// </summary>
    public sealed class ApplicationRole : IdentityRole
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationRole"/> class.
        /// </summary>
        public ApplicationRole()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationRole" /> class.
        /// </summary>
        /// <param name="name">The role name.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public ApplicationRole(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NormalizedName = name.ToUpperInvariant();
        }
    }
}
