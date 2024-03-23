
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NSwag.Annotations;
using SasavnServer.ApiClasses;
using SasavnServer.Controllers.Resellers;
using SasavnServer.Controllers.Users;
using SasavnServer.Repositories;
using SasavnServer.Service;


namespace SasavnServer.Controllers.Money
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoneyController : ControllerBase
    {
		private readonly LoggerService<MoneyController> logger;
        private readonly ITransactionRepository transactionRepository;
        private readonly IUserRepository userRepository;
        private readonly UsersService usersService;

        public MoneyController(
			ITransactionRepository transactionRepository, 
			IUserRepository userRepository, 
			UsersService usersService,
			LoggerService<MoneyController> logger)
        {
			this.logger = logger;
            this.usersService = usersService;
            this.transactionRepository = transactionRepository;
            this.userRepository = userRepository;
        }

        public class NotifyResponse
        {
            public string? merchant_id { get; set; }
            public string? transaction_id { get; set; }
            public string? pay_id { get; set; }
            public string? amount { get; set; }
            public string? currency { get; set; }
            public string? profit { get; set; }
            public string? email { get; set; }
            public string? method { get; set; }
            public string? status { get; set; }
            public string? test { get; set; }
            public string? creation_date { get; set; }
            public string? completion_date { get; set; }
            public string? sign { get; set; }
            public string? product_id { get; set; }
            public string? TSHash { get; set; }
        }

        [Authorize]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpGet("CreateTransaction/{ProductId}")]
        public string CreateTransaction(Subscription.GameIdentificator ProductId)
        {

            var user = HttpContext.User.GetUserData();

            //TODO: в жсон 
            var valuesToPay = new[] { 700 };

            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rand_string = new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());


            var TSHash = Cryptography.sha256(user.Login + rand_string);

            var response = new
            {
                merchant_id = 12459,
                pay_id = "",
                amount = valuesToPay[(int)ProductId],
                currency = "RUB",
                desc = "SUS_UVN",
                email = user.Email,
                lang = "ru",
                sign = "",
                TShash = TSHash
            };

            var addedTrans = transactionRepository.Add(new Transaction
            {
                Status = "Pending",
                UserId = user.Id,
                Date = DateTime.Now,
                TSHash = TSHash,
                Value = valuesToPay[(int)ProductId],
            });


            var sign = Cryptography.md5($"{response.currency}:{response.amount}:{"___SEC_AMOGUS___"}:{response.merchant_id}:{addedTrans.Id}");


            response = response with { sign = sign };

            var requestUrl = $"merchant?merchant_id={response.merchant_id}&pay_id={addedTrans.Id}&currency={response.currency}&amount={response.amount}&email={response.email}&sign={response.sign}&lang={response.lang}&product_id={ProductId}&TSHash={TSHash}";

			logger.Info(new LogParams {
				Title = "Create transaction",
				UserId = user.Id,
				Message = new {
					requestUrl
				},
				Importance = Importance.Extremely
			});
			
            return requestUrl;
        }

		[OpenApiIgnore]
        [HttpPost("Notify")]
        public async Task<IActionResult> Notify([FromForm] NotifyResponse notify)
        {
			logger.Info(new LogParams {
				Title = "Смотрим",
				Message = notify,
				Importance = Importance.Extremely
			});

            if (notify.status == "paid")
            {
                var trans = await transactionRepository.GetAll().SingleAsync(t => t.TSHash == notify.TSHash);


                if (notify.product_id == "0")
                {
					try {

						trans.Status = "Success";
						await GTAVSub(notify, trans);
						transactionRepository.Update(trans);
						return Ok("success");
					}
					catch (Exception ex) {

						logger.Error(new LogParams {
							Title = "Payment failed",
							Message = ex,
							Importance = Importance.Extremely
						});

           				return BadRequest(new ErrorCode(-1, JsonConvert.SerializeObject(notify)));
					}
                }
            }
            return BadRequest(new ErrorCode(-1, JsonConvert.SerializeObject(notify)));
        }

		[OpenApiIgnore]
        private async Task GTAVSub(NotifyResponse notify, Transaction trans)
        {
			await userRepository.BeginTransaction(async (_trans) => {
				var user = await _trans
					.All()
					.SingleAsync(u => u.Id == trans.UserId);

				var newSub = new Subscription
				{
					ActivationDate = DateTime.Now,
					ExpirationDate = DateTime.MinValue,
					GameId = (Subscription.GameIdentificator)long.Parse(notify.product_id),
					UserId = trans.UserId,
				};

				string newkey = Usefull.GenKey(KeyType.Default);

				var keyCode = new KeyCodes
				{
					KeyName = newkey,
					Reseller = "MrSpavn",
					Permissions = 1,
					Activatedby = user.Login
				};

				_trans.Add(keyCode);

				await usersService.ActivateKey(newkey, user);

				_trans.Add(newSub);
			});

        }


        [HttpGet("CheckPayment/{TSHash}")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Transaction))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        public async Task<ActionResult<Transaction>> CheckPayment(string TSHash)
        {
            return Ok(await transactionRepository.GetByTSHashAsync(TSHash));
        }

        [Authorize]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Transaction))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpGet("GetTransactionByUserId")]
        public async Task<ActionResult<Transaction>> GetTransactionByUserId()
        {
            var user = HttpContext.User.GetUserData();
            var trans = await transactionRepository
				.GetAll()
				.Where(t => t.UserId == user.Id)
				.ToArrayAsync();

            if (trans != null)
                return Ok(trans.Last());

            return BadRequest(new ErrorCode(-1, "trans is null"));

        }

    }
}
