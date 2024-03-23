
using Grpc.Net.Client;
using static Api.LogsService;
using Newtonsoft.Json;
using SasavnServer.Repositories;

namespace SasavnServer.Service
{

	public enum LogFlags { None = 0 }

	public enum Importance {
		Extremely = 3,
		Very = 2,
		Important = 1,
		Default = 0,
		NotImportant = -1,
		NotVery = -2,
		NotExtremely = -3
	}

	public class LogParams {

		public class AdditionalData_ {
			public List<LogFlags> Flags { get; set; } = new List<LogFlags> { LogFlags.None };
			public string Message { get; set; }
		}

		public string Title { get; set; } = "";
		public long? UserId { get; set; } = null;
		public object? Message { get; set; }
		public AdditionalData_? AdditionalData {get; set; } = null;
		public Importance Importance { get; set; } = Importance.Default;
	}

	
	public class LoggerService<T> : IDisposable where T : class {

		private readonly GrpcChannel channel;
		private readonly LogsServiceClient client;
		private readonly IHttpContextAccessor accessor;

		public LoggerService(
			IConfiguration configuration,
			IHttpContextAccessor accessor
		) {

			try {
				this.accessor = accessor;
				
				channel = GrpcChannel.ForAddress(configuration["LOGS_URL"]!);
				client = new LogsServiceClient(channel);

			}
			catch (Exception ex) {
				Console.WriteLine("Failed to init grpc channel");
			}
		}

		public void Info(LogParams logParams) {
			_log(
				level: 1, 
				messageBody: FormatMessageBody(logParams), 
				logParams.Importance
			);
		}

		public void Debug(LogParams logParams) {
			_log(
				level: 0, 
				messageBody: FormatMessageBody(logParams), 
				logParams.Importance
			);
		}

		public void Warn(LogParams logParams) {
			_log(
				level: 2, 
				messageBody: FormatMessageBody(logParams), 
				logParams.Importance
			);
		}

		public void Error(LogParams logParams) {
			_log(
				level: 3, 
				messageBody: FormatMessageBody(logParams), 
				logParams.Importance
			);
		}

		private Api.Log.Types.MessageBody FormatMessageBody(LogParams logParams) {

			User? userData = null;

			try {
				userData = accessor?.HttpContext?.User.GetUserData();
			}
			catch { }
			
			var message = logParams.Message is string
				? logParams.Message as string
				: JsonConvert.SerializeObject(logParams.Message);

			string FormatAdditionalData() {
				try {
					var request = accessor.HttpContext.Request;
					var additionalData = new {
						request.Query, 
						request.Path, 
						request.Method, 
						request.Cookies, 
						request.Host, 
						Connection = new {
							request.HttpContext.Connection.Id,
							RemoteIpAddress = request.Headers["X-Real-IP"],
							request.HttpContext.Connection.RemotePort,
						},
						User = accessor.HttpContext.User.GetUserData(),
						accessor.HttpContext.Request.Headers,
					};
					return JsonConvert.SerializeObject(additionalData);
				}
				catch(Exception ex) {
					Console.WriteLine(ex);
					return "Exception occurs";
				}


			}


			var messageBody = new Api.Log.Types.MessageBody {
				Message = message,
				Title = logParams.Title,
				UserId = new Api.Log.Types.MessageBody.Types.UserId {
					UserId_ = userData != null ? userData.Id : -1
				},
				AdditionalData = new Api.Log.Types.MessageBody.Types.AdditionalData {
					Message = FormatAdditionalData()
				}
			};

			if(logParams.AdditionalData is not null) {

				messageBody.AdditionalData.Flags.AddRange(
					logParams.AdditionalData.Flags.Select(f => (int)f)
				);
			}


			return messageBody;

		}

		private void _log(int level, Api.Log.Types.MessageBody messageBody, Importance importance = Importance.Default) {

			try {
				client.Log(new Api.LogRequest {
					Log = new Api.Log {
						
						ServiceName = "Main",
						ControllerName = typeof(T).Name,
						Level = level,
						Message = messageBody,
						Importance = (int)importance,
						Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
					}
				});
			}
			catch (Exception ex) {
				Console.WriteLine("Failed send logs to service");
			}

		}



		
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

	}
}