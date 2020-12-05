using Microsoft.AspNetCore.Mvc;

namespace ErazerShop.Bff.Controller
{
    [Route("user")]
    public class UserController : ControllerBase
    {
        [Route("info")]
        public IActionResult GetUser()
        {
            var user = new { name = User.Identity.Name };

            return new JsonResult(user);
        }

        [Route("logout")]
        public IActionResult Logout()
        {
            return SignOut("cookies", "oidc");
        }
    }
}