using Microsoft.AspNetCore.Identity;

namespace ClarityRecords.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public bool RequirePasswordChange { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
