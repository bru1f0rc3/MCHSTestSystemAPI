using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MCHSWebAPI.Controllers;

/// <summary>
/// Базовый класс для контроллеров, которым нужно знать,
/// какой пользователь сейчас выполняет запрос
/// </summary>
public abstract class AuthorizedControllerBase : ControllerBase
{
    /// <summary>
    /// Достаёт номер (id) текущего пользователя из его токена.
    /// Если номера нет или он некорректен — выбрасывает ошибку доступа
    /// </summary>
    protected int GetUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out var id))
            throw new UnauthorizedAccessException("Идентификатор пользователя отсутствует или некорректен");
        return id;
    }
}
