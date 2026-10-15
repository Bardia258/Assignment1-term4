using System.ComponentModel.DataAnnotations;

namespace MunicipalElections.Web.Areas.Admin.Models;

public sealed class AdminLoginViewModel
{
    [Required, StringLength(256), Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
