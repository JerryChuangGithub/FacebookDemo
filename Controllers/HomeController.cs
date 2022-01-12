using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using FacebookDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FacebookDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult GetToken(string name)
        {
            return Content($"hello {name}");
        }
        
        private ThirdPartyApiResultEntity<string> GetFanPageAccessToken(string fanPageId, string userToken)
        {
            var result = new ThirdPartyApiResultEntity<string>();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://graph.facebook.com");
                client.DefaultRequestHeaders.Accept.Clear();
                string exchangeToeknUrl = $@"/{fanPageId}?access_token={userToken}&fields=access_token";

                var response = client.GetAsync(exchangeToeknUrl).Result;

                JObject responseJson;
                if (response.IsSuccessStatusCode == false)
                {
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    responseJson = errorContent.ToTypedObject<JObject>();
                    var error = responseJson["error"].ToString().ToTypedObject<FacebookErrorResponseEntity>();
                    var errorMessage = error.ErrorUserMessage ?? error.Message;

                    result.Status = ThirdPartyApiResultStatusEnum.Failure;
                    result.ErrorMessage = errorMessage;
                    return result;
                }

                var responseContent = response.Content.ReadAsStringAsync().Result;
                responseJson = responseContent.ToTypedObject<JObject>();
                var fanPageToken = responseJson["access_token"].ToString();

                result.Status = ThirdPartyApiResultStatusEnum.Success;
                result.Data = fanPageToken;

                return result;
            }
        }
    }

    public static class JsonExtensions
    {
        /// <summary>
        /// To the Json String.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>Json String</returns>
        public static string ToJson(this object target)
        {
            return JsonConvert.SerializeObject(target);
        }

        /// <summary>
        /// To the typed object from string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s">The s.</param>
        /// <returns>TypedObject</returns>
        public static T ToTypedObject<T>(this string s)
        {
            if (Regex.IsMatch(s, @"^(\[|\{)(.|\n)*(\]|\})$", RegexOptions.Compiled) == false)
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(s);
        }

        /// <summary>
        /// To the typed object from stream
        /// </summary>
        /// <param name="s"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ToTypedObject<T>(this Stream s)
        {
            using (var reader = new StreamReader(s))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var ser = new JsonSerializer();
                return ser.Deserialize<T>(jsonReader);
            }
        }
    }

    public class ThirdPartyApiResultEntity<T>
    {
        /// <summary>
        /// API 結果狀態
        /// </summary>
        public ThirdPartyApiResultStatusEnum Status { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 回傳資料
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 非200情境下，Api回傳的完整資訊
        /// </summary>
        [JsonIgnore]
        public JObject ErrorResponse { get; set; }
    }

    public enum ThirdPartyApiResultStatusEnum
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success,

        /// <summary>
        /// 失敗
        /// </summary>
        Failure
    }

    public class FacebookErrorResponseEntity
    {
        /// <summary>
        /// 錯誤描述
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// 錯誤類型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 回應用戶的顯示訊息
        /// </summary>
        [JsonProperty("error_user_msg")]
        public string ErrorUserMessage { get; set; }
    }
}