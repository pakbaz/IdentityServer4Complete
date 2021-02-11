using IdentityServer.Data;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class PasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public PasswordValidator(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            this._signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            this._userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {

            ApplicationUser user = await _userManager.FindByNameAsync(context.UserName) ?? await _userManager.FindByEmailAsync(context.UserName);

            if (user == null)
            {
                context.Result = 
                    new GrantValidationResult(
                        TokenRequestErrors.InvalidGrant,
                        "Invalid username or password");
                return;
            }

            SignInResult signInResult = await _signInManager.CheckPasswordSignInAsync(user, context.Password, false);
            if (signInResult.Succeeded)
            {
                context.Result = new GrantValidationResult(
                    subject: user.UserName,
                    authenticationMethod: GrantType.ResourceOwnerPassword);
            }
            else
            {
                context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "Invalid username or password");
            }


        }
    }
}
