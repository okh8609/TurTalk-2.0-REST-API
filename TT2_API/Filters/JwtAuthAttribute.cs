using JWT;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using TT2_API.Models;
using TT2_API.Services;

namespace TT2_API.Filters
{
    public class JwtAuthAttribute : AuthorizeAttribute
    {
        public string ErrorMessage { get; set; } = "";
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            if (string.IsNullOrEmpty(ErrorMessage) == false)
            {
                setErrorResponse(actionContext, ErrorMessage);
            }
            else
            {
                base.HandleUnauthorizedRequest(actionContext);
            }
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // TODO: key應該移至config
            if (actionContext.Request.Headers.Authorization == null || actionContext.Request.Headers.Authorization.Scheme != "Bearer")
            {
                setErrorResponse(actionContext, "沒有看到存取權杖錯誤");
            }
            else
            {
                try
                {
                    #region 進行存取權杖的解碼
                    string secretKey = MainHelper.SecretKey;
                    var json = new JwtBuilder()
                        .WithAlgorithm(new HMACSHA256Algorithm())
                        .WithSecret(secretKey)
                        .MustVerifySignature()
                        .Decode<Dictionary<string, object>>(actionContext.Request.Headers.Authorization.Parameter);
                    #endregion

                    #region 將存取權杖所夾帶的內容取出來
                    var fooRole = json["role"] as Newtonsoft.Json.Linq.JArray;
                    var fooRoleList = fooRole.Select(x => (string)x).ToList<string>();
                    #endregion

                    #region 將存取權杖的夾帶欄位，儲存到 HTTP 要求的屬性
                    actionContext.Request.Properties.Add("user", json["iss"] as string);
                    actionContext.Request.Properties.Add("role", fooRoleList);
                    #endregion

                    #region 設定目前 HTTP 要求的安全性資訊
                    var fooPrincipal =
                        new GenericPrincipal(new GenericIdentity(json["iss"] as string, "MyPassport"), fooRoleList.ToArray());
                    if (HttpContext.Current != null)
                    {
                        HttpContext.Current.User = fooPrincipal;
                    }
                    #endregion

                    #region 角色權限檢查(檢查控制器或動作之屬性(Attribute上設的 Roles的設定內容)
                    if (string.IsNullOrEmpty(Roles) == false)
                    {
                        // 是否有找到匹配的角色設定
                        bool fooCheckRoleResult = false;
                        // 切割成為多個角色成員
                        var fooConditionRoles = Roles.Split(',');
                        // 逐一檢查，這個使用用者是否有在這個角色條件中
                        foreach (var item in fooConditionRoles)
                        {
                            var fooInRole = fooPrincipal.IsInRole(item.Trim());
                            if (fooInRole == true)
                            {
                                fooCheckRoleResult = true;
                                break;
                            }
                        }

                        if (fooCheckRoleResult == false)
                        {
                            setErrorResponse(actionContext, "無效的角色設定，沒有權限使用這個 API");
                        }
                    }
                    #endregion

                }
                catch (TokenExpiredException)
                {
                    setErrorResponse(actionContext, "權杖已經逾期");
                }
                catch (SignatureVerificationException)
                {
                    setErrorResponse(actionContext, "權杖不正確，沒有正確的數位簽名");
                }
                catch (Exception ex)
                {
                    setErrorResponse(actionContext, $"權杖解析發生異常 : {ex.Message}");
                }
            }

            base.OnAuthorization(actionContext);
        }

        private void setErrorResponse(HttpActionContext actionContext, string message)
        {
            ErrorMessage = message;
            var response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, message);
            response.Content = new ObjectContent<APIResult>(new APIResult()
            {
                Success = false,
                Message = ErrorMessage,
                Payload = null
            }, new JsonMediaTypeFormatter());
            actionContext.Response = response;
        }
    }
}