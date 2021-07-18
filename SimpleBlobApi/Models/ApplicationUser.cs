using Fusi.Api.Auth.Models;

namespace SimpleBlobApi.Models
{
    /// <summary>
    /// Application user. Note that even if the model is equal to
    /// <see cref="NamedUser"/>, we need to have the user class named this way
    /// or the PostgresSql driver will assume a different name for the users
    /// table.
    /// </summary>
    /// <seealso cref="NamedUser" />
    public class ApplicationUser : NamedUser
    {
    }
}
