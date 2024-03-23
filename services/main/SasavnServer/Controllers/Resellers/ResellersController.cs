using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SasavnServer.ApiClasses;
using SasavnServer.Model;
using SasavnServer.Repositories;
using SasavnServer.Service;

namespace SasavnServer.Controllers.Resellers
{
	
    [Route("api/[controller]")]
	[Authorize]
	[ClaimRequirement(ClaimTypes.Role, "Reseller")]
    [ApiController]
	
    public class ResellersController : ControllerBase
    {
		private readonly LoggerService<ResellersController> logger;
        private readonly IUserRepository userRepository;

        public ResellersController(
			IUserRepository userRepository,
			LoggerService<ResellersController> logger
			)
        {
			this.logger = logger;
            this.userRepository = userRepository;
        }

        [HttpGet("GetReseller")]
		public async Task<Reseller?> GetReseller() 
		{
			var id = HttpContext.User?.Identities.First();
            var resellerFromId = Reseller.FromIdentity(id);
           	return await userRepository
				.Resellers()
				.SingleAsync(r => r.Id == resellerFromId.Id);
		}

		private async Task<Reseller?> _getReseller(ClaimsPrincipal? principal) 
		{
			var id = principal?.Identities.First();
            var resellerFromId = Reseller.FromIdentity(id);
           	return await userRepository
				.Resellers()
				.SingleAsync(r => r.Id == resellerFromId.Id);
		}

        [HttpPost("getKeysForReseller")]
        public async Task<ActionResult<GetKeysForResellerResult>> GetKeysForReseller([FromBody] GetKeysForResellerModel model)
        {

            var reseller = await _getReseller(HttpContext.User);

			var keys_ = userRepository
				.GetAllKeys()
				.Where(k => k.Reseller == reseller.Login && k.Permissions == (int)model.KeyType);
				
			var keys = new List<string>();

			foreach (var key in keys_)
			{
				keys.Add(key.KeyName);
			}

			return Ok(new GetKeysForResellerResult {
				Keys = keys
			});
        }

        [HttpGet("getCountKeysOfReseller")]
        public async Task<ActionResult<int>> GetCountKeysOfReseller()
        {
            var reseller = await _getReseller(HttpContext.User);

            var keyCodes = userRepository.GetAllKeys();

			var keysCount = userRepository
				.GetAllKeys()
				.Count(key => key.Reseller == reseller.Login && key.Activated == 0);

			return Ok(keysCount);
        }

   	 	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpPost("genKey")]
        public async Task<ActionResult> GenKey([FromBody] GenKeyModel model)
        {
            var reseller = await _getReseller(HttpContext.User);

			return userRepository.BeginTransaction<ActionResult>((trans) => {

 				int avalable_keys = model.KeyType == KeyType.HWID 
					? reseller.AvailableHwidKeys 
					: reseller.AvailableKeys;

                if (avalable_keys < model.KeysAmount)
                    return BadRequest(new ErrorCode(-2, "Trying to generate more keys than can"));

                if (model.KeysAmount < 1)
                    return BadRequest(new ErrorCode(-3, "WTF ARE YOU DOING?"));

                if (model.KeyType == KeyType.HWID)
                    reseller.AvailableHwidKeys -= model.KeysAmount;
                else
                    reseller.AvailableKeys -= model.KeysAmount;


                trans.Update(reseller);
				
				logger.Info(new LogParams {
					Message = $"Generate keys for reseller: {reseller.Login}",
					Importance = Importance.Extremely
				});
				
				foreach(var _ in Enumerable.Range(0, model.KeysAmount)) {

                    trans.Add(new KeyCodes
                    {
                        KeyName = Usefull.GenKey(model.KeyType),
                        Reseller = reseller.Login,
                        Permissions = (int)model.KeyType,
                        Activatedby = ""
                    });
				}
				
				return Ok();
			});

        }
    }
}
