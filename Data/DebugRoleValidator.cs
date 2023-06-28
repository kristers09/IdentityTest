// using System.Globalization;
// using System.Runtime.Versioning;
// using Microsoft.AspNetCore.Identity;

// namespace IdentityTest.Data;

// public class DebugRoleValidator : RoleValidator<IdentityRole>
// {
//     public DebugRoleValidator(IdentityErrorDescriber? errors = null)
//         : base(errors) { }

//     private RoleManager<IdentityRole> Manager { get; set; }

//     public override async Task<IdentityResult> ValidateAsync(
//         RoleManager<IdentityRole> manager,
//         IdentityRole role
//     )
//     {
//         //
//         var result = await base.ValidateAsync(manager, role);

//         return result;
//     }

//     public async Task ValidateRoleName(IdentityRole role, List<string> errors)
//     {
//         if (string.IsNullOrWhiteSpace(role.Name))
//         {
//             errors.Add(
//                 String.Format(CultureInfo.CurrentCulture, Resources.PropertyTooShort, "Name")
//             );
//         }
//         else
//         {
//             var owner = await Manager.FindByNameAsync(role.Name).WithCurrentCulture();
//             if (owner != null && !EqualityComparer<Guid>.Default.Equals(owner.Id, role.Id))
//             {
//                 errors.Add(
//                     String.Format(CultureInfo.CurrentCulture, Resources.DuplicateName, role.Name)
//                 );
//             }
//         }
//     }
// }
