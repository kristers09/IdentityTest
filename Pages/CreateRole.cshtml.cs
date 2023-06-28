using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityTest.Pages;

public class CreateRoleModel : PageModel
{
    private readonly ILogger<CreateRoleModel> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;

    public CreateRoleModel(ILogger<CreateRoleModel> logger, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _roleManager = roleManager;
    }

    public IActionResult OnGet()
    {
        _logger.LogInformation("OnGetAsync()");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("OnPostAsync()");

        var result = await _roleManager.CreateAsync(new IdentityRole("Administrator"));

        if (result.Succeeded)
        {
            _logger.LogInformation("YAY!");
        }
        else
        {
            foreach (IdentityError error in result.Errors)
            {
                _logger.LogError(error.Description);
            }
        }

        return Page();
    }
}
