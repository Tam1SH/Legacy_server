using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SasavnServer.ApiClasses;
using SasavnServer.Controllers.Users;
using SasavnServer.Repositories;

namespace SasavnServer.Controllers.Admin
{


	[Authorize]
	[ClaimRequirement(ClaimTypes.Role, "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : Controller
    {
        private readonly AdminService adminService;

        public AdminController(
            AdminService adminService)
        {
            this.adminService = adminService;
        }


        [HttpPost("changeUser")]
        public async Task ChangeUser()
        {
            int? Emailaccepted = null;

            if (int.TryParse(Request.Form["role"], out var emailaccepted))
            {
                Emailaccepted = emailaccepted;
            }

            int? VKId = null;
            if (int.TryParse(Request.Form["vkId"], out var vKId))
            {
                VKId = vKId;

            }

            await adminService.ChangeUser(new PartialUser
            {
                Id = long.Parse(Request.Form["id"]),
                Login = Request.Form["login"],
                Password = Request.Form["pass"],
                Email = Request.Form["email"],
                Role = int.Parse(Request.Form["role"]),
                AuthToken = Request.Form["authToken"],
                money = Request.Form["money"],
                Emailaccept = Request.Form["emailaccept"],
                Emailaccepted = Emailaccepted,
                Avatar = Request.Form["avatar"],
                DSUserID = Request.Form["dsUserID"],
                DSUserName = Request.Form["dsUserName"],
                VKId = VKId,
                VKTempKey = Request.Form["vkTempKey"],
            });
			
        }


		[HttpGet("getLogs")]
        public async Task<ActionResult<string>> GetLogs()
        {
            var logs = await adminService.GetLogs();
			
			if(logs is null)
				return BadRequest(new ErrorCode(-1, ""));

            return Ok(logs);

        }

        [HttpGet("changeCheatVersion/{newVersion}")]
        public async Task ChangeCheatVersion(string newVersion)
        {
            await adminService.ChangeCheatVersion(newVersion);
			
        }

        [HttpGet("changeLauncherVersion/{newVersion}")]
        public async Task ChangeLauncherVersion(string newVersion)
        {
            await adminService.ChangeLauncherVersion(newVersion);
        }

        [HttpGet("getMainInfoForAdmin")]
        public async Task<AllMainInfoAboutSHBHLOL> GetMainInfoForAdmin()
        {
            return await adminService.AllMainInfo();
        }

        [HttpGet("getStats")]
        public async Task<Stats[]> GetStats()
        {
            return await adminService.GetStats();
        }

        [HttpGet("getUsersOnline")]
        public async Task<TotalUser[]> GetUsersOnline()
        {
            return await adminService.GetUsersOnlineAsync();
        }

        [HttpGet("getAllUsers")]
        public async Task<TotalUser[]> GetAllUsersAsync()
        {
            return await adminService.GetAllUsersAsync();
        }

        [HttpGet("getUsersByIds/{offset}/{count}")]
        public User[] GetUsersByIds(int offset, int count)
        {
            return adminService.getUsersById(offset, count);
        }
    }
}
