using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Bogus.DataSets;
using Newtonsoft.Json;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace Quarkless.Creator
{
	#region Models
	internal class WebCaller : WebClient
	{
		protected override WebRequest GetWebRequest(Uri address)
		{
			var req = (HttpWebRequest)base.GetWebRequest(address);
			req.KeepAlive = false;

			return req;
		}
	}
	public class Proxy
	{
		public string IP { get; set; }
		public int Port { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
	}
	public class FakerModel
	{
		public Name.Gender Gender { get; set; }
		public string Email { get; set; }
		public string Username { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Password { get; set; }
		public string UserAgent { get; set; }
		public Proxy Proxy { get; set; }
	}
	public class Payload
	{
		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("password")]
		public string Password { get; set; }

		[JsonProperty("username")]
		public string Username { get; set; }

		[JsonProperty("first_name")]
		public string FirstName { get; set; }

//		[JsonProperty("client_id")]
//		public string ClientId { get; set; }

		[JsonProperty("seamless_login_enabled")]
		public string SeamlessLoginEnabled { get; set; } = "1";

//		[JsonProperty("gdpr_s")]
//		public string GDPRS { get; set; }

		[JsonProperty("tos_version")]
		public string TOSVersion { get; set; }

		[JsonProperty("opt_into_one_tap")]
		public bool OptIntoOneTap { get; set; } = false;

	}
	public struct PassData
	{
		public Dictionary<string, string> Headers;
		public IEnumerable<Cookie> Cookies;

		public PassData(Dictionary<string, string> headers, IEnumerable<Cookie> cookies)
		{
			Headers = headers;
			Cookies = cookies;
		}
	}
	#endregion

	public class Creator
	{
		private readonly Random _random;
		private const string BASE_URL = "https://www.instagram.com";
		private readonly string CREATE_ACC_AJAX_URL = $"{BASE_URL}/accounts/web_create_ajax/";
		public Creator()
		{
			_random = new Random();
		}
		public async Task CreateAccountWithBrowserBrowser(FakerModel person)
		{
			//Used initially to download the chromium drive, afterwards can comment out
			//await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);

			var options = new LaunchOptions
			{
				Headless = false,
				Args = new []
					{
						"--disable-infobars",
						"--window-position=0,0",
						"--ignore-certifcate-errors",
						"--ignore-certifcate-errors-spki-list",
						"--no-sandbox", 
						"--disable-setuid-sandbox"
					},
				IgnoreHTTPSErrors = true,
				UserDataDir = "./tmp"
			};

			if(person.Proxy!=null)
				options.Args[options.Args.Length] = $"--proxy-server=http://{person.Proxy.IP}:{person.Proxy.Port}";

			using (var browser = await Puppeteer.LaunchAsync(options))
			using (var page = await browser.NewPageAsync())
			{
				await page.EvaluateFunctionOnNewDocumentAsync(@"
					() => {
						Object.defineProperty(navigator, 'languages', {
							get: function() {
								return ['en-US', 'en'];
							},
						});

						Object.defineProperty(navigator, 'webdriver', {
						get: () => undefined,});

						Object.defineProperty(navigator, 'plugins', {
							get: function() {
								return [1, 2, 3, 4, 5];
							},
						});
					}
				");

				await page.SetUserAgentAsync(person.UserAgent);
				var typeOptions = new TypeOptions
				{
					Delay = _random.Next(20, 100)
				};
				
				if(person.Proxy !=null)
					await page.AuthenticateAsync(new Credentials {Password = person.Proxy.Password, Username = person.Proxy.Username});
				
				await page.GoToAsync(BASE_URL);

				#region Click on cookies notification

				try
				{
					var selector = ".KPZNL";
					var element = await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
					{
						Timeout = 1250
					});
					var boelement = await element.BoundingBoxAsync();
					await page.Mouse.MoveAsync(boelement.X, boelement.Y);
					await element.ClickAsync();
				}
				catch
				{

				}

				#endregion

				await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 2) + _random.NextDouble()));

				#region Name Field
				var emailField = await page.WaitForSelectorAsync("input[name=emailOrPhone]");
				var bnameBox = await emailField.BoundingBoxAsync();
				await page.Mouse.MoveAsync(bnameBox.X, bnameBox.Y);
				await emailField.ClickAsync();
				#endregion
				
				#region Email Field
				await emailField.TypeAsync(person.Email, typeOptions);
				await page.Keyboard.DownAsync("Tab");
				#endregion

				#region Full-Name Field
				var fullNameField = await page.WaitForSelectorAsync("input[name=fullName]");
				await fullNameField.TypeAsync(person.FirstName + " " + person.LastName, typeOptions);
				await page.Keyboard.DownAsync("Tab");
				#endregion

				#region Username Field
				var usernameField = await page.WaitForSelectorAsync("input[name=username]");
				await usernameField.TypeAsync(person.Username, new TypeOptions{Delay = _random.Next(25,100)});
				await page.Keyboard.DownAsync("Tab");
				#endregion

				#region Password Field
				var passwordField = await page.WaitForSelectorAsync("input[name=password]");
				await passwordField.TypeAsync(person.Password, new TypeOptions{Delay = _random.Next(35,125)});
				#endregion

				#region Submit Button (first)
				try
				{
					var submitButton = await page.WaitForXPathAsync("//button[text()='Next']", new WaitForSelectorOptions
					{
						Timeout = 2250
					});
					var submitBounding = await submitButton.BoundingBoxAsync();
					await page.Mouse.MoveAsync(submitBounding.X, submitBounding.Y);
					await submitButton.ClickAsync();
				}
				catch
				{
					var submitButton = await page.WaitForXPathAsync("//button[text()='Sign up']", new WaitForSelectorOptions
					{
						Timeout = 2250
					});
					var submitBounding = await submitButton.BoundingBoxAsync();
					await page.Mouse.MoveAsync(submitBounding.X, submitBounding.Y);
					await submitButton.ClickAsync();
				}
				#endregion

				await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 3) + _random.NextDouble()));

				#region Is Over 18 checkbox
				try
				{
					var buttonIsOver18 = await page.WaitForSelectorAsync("#igCoreRadioButtonageRadioabove_18",
						new WaitForSelectorOptions
						{
							Timeout = 8000
						});
					var buttonIsOver18Bounding = await buttonIsOver18.BoundingBoxAsync();
					await page.Mouse.MoveAsync(buttonIsOver18Bounding.X, buttonIsOver18Bounding.Y);
					await buttonIsOver18.ClickAsync(new ClickOptions { ClickCount = 2 });
				}
				catch
				{
					Console.WriteLine("err occured");
					return;
				}
				#endregion

				#region Final Submit Button
				var submitButton2 = (await page.XPathAsync("//button[text()='Next']")).Last();
				var submitBounding2 = await submitButton2.BoundingBoxAsync();
				await page.Mouse.MoveAsync(submitBounding2.X, submitBounding2.Y);
				await submitButton2.ClickAsync();

				await Task.Delay(10000);
				try
				{
					var err = await page.WaitForSelectorAsync("#ssfErrorAlert");
					Console.WriteLine("err");
				}
				catch
				{
					Console.WriteLine("no err");
				}
				#endregion

			}
		}
		public async Task Create()
		{
			var person = GeneratePerson(emailProvider: "gmail.com");

			person.Proxy = new Proxy
			{
				IP = "194.67.193.151",
				Port = 20068,
				Username = "user8",
				Password = "dsifuys*&9ydsgd"
			};

			try
			{
				await CreateAccountWithBrowserBrowser(person);
			}
			catch (Exception ee)
			{
				Console.WriteLine(ee.Message);
			}

			#region old req code
			//var cookieHeaders = await InitialCall(person.UserAgent);
			//			var headersAndCookies = await GetHeaderCookieDetails(person.UserAgent);
			//
			//			var payload =JsonConvert.SerializeObject(new Payload
			//			{
			//				Email = person.Email,
			//				FirstName = person.FirstName,
			//				Username = person.Username,
			//				Password = person.Password,
			//				OptIntoOneTap = false,
			//				TOSVersion = "row",
			//				SeamlessLoginEnabled = "1"
			//			});
			//			var bytes = Encoding.UTF8.GetBytes(payload);
			//
			//			var ms = new MemoryStream();
			//			using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
			//				gzip.Write(bytes, 0, bytes.Length);
			//			
			//			ms.Position = 0;
			//
			//			var sContent = new StreamContent(ms);
			//			sContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			//			sContent.Headers.ContentEncoding.Add("gzip");
			//
			//			var stringContent = new StringContent(payload);
			//			stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			//stringContent.Headers.ContentEncoding.Add("gzip");
			//stringContent.Headers.Allow.Add("*/*");

			//			var cookieContainer = new CookieContainer();
			//			var handler = new HttpClientHandler()
			//			{
			//				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			//				CookieContainer = cookieContainer
			//			};
			//
			//			try
			//			{
			//				using (var client = new HttpClient(handler, false))
			//				{
			//					foreach (var valueCookie in headersAndCookies.Item1.Value.Cookies)
			//						cookieContainer.Add(valueCookie);
			//
			//					foreach (var keyValuePair in headersAndCookies.Item1.Value.Headers)
			//						client.DefaultRequestHeaders.TryAddWithoutValidation(keyValuePair.Key, keyValuePair.Value);
			//					
			//					
			//					//client.DefaultRequestHeaders.UserAgent.ParseAdd(person.UserAgent);
			//					client.DefaultRequestHeaders.Connection.Add("keep-alive");
			//					var resp = await client.PostAsync(CREATE_ACC_AJAX_URL, stringContent);
			//					//var resp = await client.SendAsync(RequestMessage(person.UserAgent, sContent));
			//					var content = await resp.Content.ReadAsStringAsync();
			//					Console.WriteLine(content);
			//				}
			//			}
			//			catch (Exception io)
			//			{
			//
			//			}
			//
			//			//client.Proxy = GetProxy();
			#endregion
		}

		public FakerModel GeneratePerson(string locale = "en", string emailProvider = null, bool? isMale = null)
		{
			var faker = new Bogus.Faker(locale);
			Name.Gender gender;
			if (isMale != null)
				gender = isMale == true ? Name.Gender.Male : Name.Gender.Female;
			else
				gender = faker.Person.Gender;

			var firstName = faker.Name.FirstName(gender);
			var lastName = faker.Name.LastName(gender);

			var userName = faker.Internet.UserName(firstName, lastName) + "." + _random.Next(1, 1050);

			var password = faker.Internet.Password(_random.Next(13, 25), true, prefix: lastName.Substring(0, lastName.Length / 2));

			var userAgent = faker.Internet.UserAgent();
			string email;
			if (emailProvider != null)
				email = faker.Internet.Email(firstName, lastName, emailProvider, (_random.Next(12, 22) + faker.UniqueIndex).ToString());
			else
			{
				email = faker.Internet.Email(firstName, lastName,
					(_random.Next(13, 23) + faker.UniqueIndex).ToString());
			}

			return new FakerModel
			{
				Email = email,
				FirstName = firstName,
				Gender = gender,
				LastName = lastName,
				Password = password,
				Username = userName,
				UserAgent = userAgent
			};
		}

		#region Request Based (doesn't work)
		public WebHeaderCollection DefaultHeaders(string userAgent, int contentLength)
		{
			return new WebHeaderCollection
			{
				new NameValueCollection
				{
					{ "Accept" , "*/*"},
					{ "Accept-Encoding", "gzip, deflate, br"},
					{ "Accept-Language", "en-US,en;q=0.5" },
					//{ "Connection", "keep-alive" },
					{ "Content-Length", contentLength.ToString() },
					{ "Content-Type", "application/x-www-form-urlencoded" },
					{ "Cookie", "csrftoken=lBJeO6SkVQPGhi9laEIgEDtZzbNEzFuE; rur=ATN; mid=XZwgIgALAAG7gFjKoQVS2rn39DH3" },
					{ "Host", "www.instagram.com" },
					{ "Proxy-Authorization", "Basic dXNlcjg6ZHNpZnV5cyomOXlkc2dk" },
					{ "Referer", "https://www.instagram.com/?hl=en" },
					{ "TE", "Trailers" },
					{ "User-Agent", userAgent },
					{ "X-CSRFToken", "lBJeO6SkVQPGhi9laEIgEDtZzbNEzFuE" },
					{ "X-IG-App-ID", "936619743392459" },
					{ "X-IG-WWW-Claim", "0" },
					{ "X-Instagram-AJAX", "88507d5bfa12" },
					{ "X-Requested-With", "XMLHttpRequest" }
				}
			};
		}
		public HttpRequestMessage RequestMessage(string userAgent, StreamContent data)
		{
			var httpReqMessage = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(CREATE_ACC_AJAX_URL),
				Headers =
				{
					//{"Accept", "*/*"},
					//{"Accept-Encoding", "gzip, deflate, br"},
					{"Accept-Language", "en-US,en;q=0.5"},
					{ "Connection", "keep-alive" },
					//{"Content-Length", data.Length.ToString()},
					//{"Content-Type", "application/x-www-form-urlencoded"},
					{"Cookie", "csrftoken=lBJeO6SkVQPGhi9laEIgEDtZzbNEzFuE; rur=ATN; mid=XZwgIgALAAG7gFjKoQVS2rn39DH3"},
					{"Host", "www.instagram.com"},
					{"Proxy-Authorization", "Basic dXNlcjg6ZHNpZnV5cyomOXlkc2dk"},
					{"Referer", "https://www.instagram.com/"},
					{"TE", "Trailers"},
					{"User-Agent", userAgent},
					{"X-CSRFToken", "lBJeO6SkVQPGhi9laEIgEDtZzbNEzFuE"},
					{"X-IG-App-ID", "936619743392459"},
					{"X-IG-WWW-Claim", "0"},
					{"X-Instagram-AJAX", "88507d5bfa12"},
					{"X-Requested-With", "XMLHttpRequest"}
				},
				Content = data
			};
			return httpReqMessage;
		}
		public async Task<(PassData?, PassData?)> GetHeaderCookieDetails(string userAgent)
		{
			PassData? passReq = null;
			PassData? passResp = null;
			//await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
			var options = new LaunchOptions
			{
				Headless = false
			};
			using (var browser = await Puppeteer.LaunchAsync(options))
			using (var page = await browser.NewPageAsync())
			{
				await page.SetUserAgentAsync(userAgent);
				await page.SetRequestInterceptionAsync(true);
				var firstPassReq = false;
				var firstPassResp = false;
				page.Request += async (o, e) =>
				{
					await e.Request.ContinueAsync();
					//await e.Request.
				};

				await page.GoToAsync(BASE_URL);
				var selector = ".KPZNL";
				var element = await page.WaitForSelectorAsync(selector);
				await element.ClickAsync();
				await page.ReloadAsync();
				await Task.Delay(500);
				var cookies = await page.GetCookiesAsync();

				page.Response += (o, e) =>
				{
					if (firstPassResp) return;
					firstPassResp = true;
					passResp = new PassData(e.Response.Headers,
						cookies.Select(x => new Cookie(x.Name, x.Value, x.Path, x.Domain)));
				};
				page.RequestFinished += (o, e) =>
				{
					if (firstPassReq) return;
					firstPassReq = true;
					passReq = new PassData(e.Request.Headers,
						cookies.Select(x => new Cookie(x.Name, x.Value, x.Path, x.Domain)));
				};

				await page.WaitForResponseAsync(_ => _.Ok);
				await page.WaitForRequestAsync(_ => _.Headers.Count > 4);
			}

			return (passReq, passResp);
		}
		public IWebProxy GetProxy()
		{
			return new WebProxy
			{
				Address = new Uri("194.67.193.151:20068"),
				Credentials = new NetworkCredential("user8", "dsifuys*&9ydsgd")
			};
		}
		#endregion

	}
}
