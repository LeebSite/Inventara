using Inventara.Application.Auth;
using Inventara.Domain.Entities;

namespace Inventara.Application.Security;

public interface IJwtTokenIssuer
{
    IssuedToken Issue(User user);
}
