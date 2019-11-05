﻿using DYSDK.Model;
using DYSDK.Request;
using DYSDK.Response;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DYSDK.Common
{
    public class DouYinClient
    {
        public const string BaseUrl = "https://open.douyin.com";

        /// <summary>
        /// 应用唯一标识
        /// </summary>
        private string Client_key { get; set; }

        /// <summary>
        /// 应用密钥
        /// </summary>
        private string Client_secret { get; set; }

        public DouYinClient(string key, string secret)
        {
            this.Client_key = key;
            this.Client_secret = secret;
        }

        #region 获取请求参数key=value的形式
        /// <summary>
        /// 获取请求类的keyValue
        /// </summary>
        /// <param name="request"></param>
        /// <param name="notDeal"></param>
        /// <returns></returns>
        private string GetRequestKeyValue(BaseRequest request)
        {
            List<string> keyVals = new List<string>();
            Type type = request.GetType();
            var propList = type.GetProperties().OrderBy(x => x.Name.ToLower()).ToList();
            propList.ForEach(p =>
            {
                JsonPropertyAttribute attr = (JsonPropertyAttribute)p.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault();
                if (attr != null)
                {
                    string key = attr.PropertyName;
                    string value = string.Empty;
                    if (p.PropertyType.IsClass && p.PropertyType != typeof(String))
                    {
                        value = JsonConvert.SerializeObject(p.GetValue(request));
                    }
                    else
                    {
                        value = p.GetValue(request) != null ? p.GetValue(request).ToString() : string.Empty;
                    }
                    switch (key)
                    {
                        case "client_key":
                            value = Client_key;
                            break;
                        case "client_secret":
                            value = Client_secret;
                            break;
                    }
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        keyVals.Add(key + "=" + value);
                    }
                }
            });
            return string.Join("&", keyVals);
        }

        private string GetRequestKeyValue(BaseRequest request,out List<KeyValuePair<string,string>> bodyData)
        {
            List<string> keyVals = new List<string>();
            List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();
            Type type = request.GetType();
            var propList = type.GetProperties().OrderBy(x => x.Name.ToLower()).ToList();
            propList.ForEach(p =>
            {
                JsonPropertyAttribute attr = (JsonPropertyAttribute)p.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault();
                if (attr != null)
                {
                    int order = attr.Order;
                    string key = attr.PropertyName;
                    string value = string.Empty;
                    if (p.PropertyType.IsClass && p.PropertyType != typeof(String))
                    {
                        value = JsonConvert.SerializeObject(p.GetValue(request));
                    }
                    else
                    {
                        value = p.GetValue(request) != null ? p.GetValue(request).ToString() : string.Empty;
                    }
                    switch (key)
                    {
                        case "client_key":
                            value = Client_key;
                            break;
                        case "client_secret":
                            value = Client_secret;
                            break;
                    }
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        // post 请求，order = 1 的字段需要
                        if (order == 1)
                        {
                            keyValues.Add(new KeyValuePair<string, string>(key,value));
                        }
                        else
                        {
                            keyVals.Add(key + "=" + value);
                        }
                    }
                }
            });
            bodyData = keyValues;
            return string.Join("&", keyVals);
        }
        #endregion

        #region 向抖音发起请求
        /// <summary>
        /// 向抖音发起请求
        /// </summary>
        /// <typeparam name="Res"></typeparam>
        /// <param name="requestParam"></param>
        /// <returns></returns>
        private Res Execute<Res>(BaseRequest requestParam)
        {
            RestClient restClient = new RestClient(BaseUrl);
            List<KeyValuePair<string, string>> bodyParam = null;
            string urlData = string.Empty;
            RestRequest request;
            if (requestParam.RequestType() == "POST")
            {
                urlData = this.GetRequestKeyValue(requestParam, out bodyParam);
            }
            else if (requestParam.RequestType() == "GET")
            {
                urlData = this.GetRequestKeyValue(requestParam);
            }
            request = new RestRequest(requestParam.ApiName() + "?" + urlData);
            request.Method = (Method)System.Enum.Parse(typeof(Method), requestParam.RequestType());
            if (bodyParam!=null && bodyParam.Count > 0)
            {
                bodyParam.ForEach(p =>
                {
                    request.Parameters.Add(new Parameter(p.Key, p.Value, ParameterType.RequestBody));
                });
            }
            IRestResponse response = restClient.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (!string.IsNullOrWhiteSpace(response.Content))
                {
                    return JsonConvert.DeserializeObject<Res>(response.Content);
                }
                else
                {
                    throw new Exception("Response.Content is null");
                }
            }
            else
            {
                throw new Exception("error code:" + response.Content);
            }

        }

        #endregion

        #region 授权相关

        #region 用户授权发起url
        /// <summary>
        /// 获取用户授权url
        /// </summary>
        /// <returns></returns>
        public string GetLogonAuthorizationUrl(string redirect_uri, string scope = "user_info,video.list,video.data")
        {
            string logonAuthorizationUrl = string.Empty;
            PlatformOauthConnectRequest request = new PlatformOauthConnectRequest()
            {
                Scope = scope
                ,
                RedirectURI = redirect_uri
            };

            return BaseUrl + request.ApiName() + "?" + GetRequestKeyValue(request);
        }
        #endregion

        #region 获取用户授权token
        /// <summary>
        /// 获取用户的授权token
        /// </summary>
        /// <param name="code">授权回调返回的code</param>
        /// <returns></returns>
        public OauthResponseData GetAccessToken(string code)
        {
            OauthAccessTokenRequest request = new OauthAccessTokenRequest()
            {
                Code = code
            };
            OauthAccessTokenResponse response = Execute<OauthAccessTokenResponse>(request);
            if (response.Message == "success")
            {
                return response.Data;
            }
            else
            {
                throw new Exception("error code:" + response.Data.ErrorCode + ",error msg:" + response.Data.Description);
            }
        }
        #endregion

        #region 刷新/续期用户授权
        /// <summary>
        /// 刷新/续期用户授权
        /// </summary>
        /// <param name="refresh_token">刷新token</param>
        /// <returns></returns>
        public OauthResponseData RefreshToken(string refresh_token)
        {
            OauthRefreshTokenRequest request = new OauthRefreshTokenRequest()
            {
                RefreshToken = refresh_token
            };

            OauthRefreshTokenResponse response = Execute<OauthRefreshTokenResponse>(request);
            if (response.Message == "success")
            {
                return response.Data;
            }
            else
            {
                throw new Exception("error code:" + response.Data.ErrorCode + ",error msg:" + response.Data.Description);
            }
        }
        #endregion

        #endregion

        #region 用户相关

        #region 获取用户公开信息
        /// <summary>
        /// 获取用户公开信息
        /// </summary>
        /// <param name="open_id">用户在应用的唯一标识</param>
        /// <returns></returns>
        public OauthUserInfoData GetUserInfo(string open_id,string access_token)
        {
            OauthUserInfoRequest request = new OauthUserInfoRequest()
            {
                OpenId = open_id
                ,AccessToken = access_token
            };

            OauthUserInfoResponse response = Execute<OauthUserInfoResponse>(request);
            if (response.Message == "success")
            {
                return response.Data;
            }
            else
            {
                throw new Exception("error code:" + response.Data.ErrorCode + ",error msg:" + response.Data.Description);
            }
        }
        #endregion

        #endregion

        #region 视频相关

        #region 获取用户已经发布得视频列表
        /// <summary>
        /// 获取用户已经发布得视频列表
        /// </summary>
        /// <param name="open_id">用户应用标识</param>
        /// <param name="access_token">用户授权token</param>
        /// <param name="cursor">分页游标, 默认传0。</param>
        /// <param name="count">每页数量</param>
        /// <returns></returns>
        public List<VideoData> GetVideoList(string open_id, string access_token, int cursor, int count)
        {
            VideoListRequest request = new VideoListRequest()
            {
                OpenId = open_id
                ,
                AccessToken = access_token
                ,
                Cursor = cursor
                ,
                Count = count
            };

            VideoListResponse response = Execute<VideoListResponse>(request);
            if (response.Message == "success")
            {
                return response.Data.List;
            }
            else
            {
                throw new Exception("error code:" + response.Data.ErrorCode + ",error msg:" + response.Data.Description);
            }
        }
        #endregion

        #region 查询指定视频数据
        /// <summary>
        /// 查询指定视频数据
        /// </summary>
        /// <param name="open_id">用户应用标识</param>
        /// <param name="access_token">用户授权token</param>
        /// <param name="item_ids">视频ids</param>
        /// <returns></returns>
        public List<VideoData> GetVideoData(string open_id,string access_token,List<string> item_ids)
        {
            VideoDataRequest request = new VideoDataRequest()
            {
                OpenId = open_id
                ,AccessToken  = access_token
                ,ItemIds = item_ids
            };

            VideoListResponse response = Execute<VideoListResponse>(request);
            if (response.Message == "success")
            {
                return response.Data.List;
            }
            else
            {
                throw new Exception("error code:" + response.Data.ErrorCode + ",error msg:" + response.Data.Description);
            }
        }
        #endregion

        #endregion

    }
}
