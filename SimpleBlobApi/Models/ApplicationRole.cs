using Microsoft.AspNetCore.Identity;

namespace SimpleBlobApi.Models
{
    /// <summary>
    /// Application role. Note that even if the model is equal to
    /// <see cref="IdentityRole"/>, we need to have the user class named this way
    /// or the PostgresSql driver will assume a different name for the roles
    /// table.
    /// </summary>
    /// <seealso cref="IdentityRole" />
    public class ApplicationRole : IdentityRole
    {
    }
}
