﻿using SimpleBlobApi.Auth;

namespace SimpleBlobApi.Services
{
    /// <summary>
    /// Seeded user options for <see cref="ApplicationUser"/>.
    /// </summary>
    public sealed class ApplicationSeededUserOptions : SeededUserOptions
    {
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string LastName { get; set; }
    }
}
