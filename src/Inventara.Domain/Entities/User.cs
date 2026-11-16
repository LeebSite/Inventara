using Inventara.Domain.Common;
using Inventara.Domain.Enums;

namespace Inventara.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }

    public string Department { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
