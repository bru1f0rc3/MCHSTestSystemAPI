using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MCHSWebAPI.Controllers;

public abstract class AuthorizedControllerBase : ControllerBase
{
    protected int GetUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out var id))
            throw new UnauthorizedAccessException("Идентификатор пользователя отсутствует или некорректен");
        return id;
    }
}
