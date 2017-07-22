﻿using ClientsLibrary;
using CommonLibrary;
using HslCommunication;
using HslCommunication.BasicFramework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace 软件系统浏览器模版.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }


        // GET: LoginPage
        public ActionResult Login(string Message)
        {
            ViewData["Message"] = Message;
            return View();
        }



        //POST 用于系统的账户登录
        // 为了防止“过多发布”攻击，请启用要绑定到的特定属性，有关 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(FormCollection fc)
        {
            //请求指令头数据，该数据需要更具实际情况更改
            OperateResultString result = UserClient.Net_simplify_client.ReadFromServer(CommonLibrary.CommonHeadCode.SimplifyHeadCode.维护检查);
            if (result.IsSuccess)
            {
                //例如返回结果为1说明允许登录，0则说明服务器处于维护中，并将信息显示
                if (result.Content != "1")
                {
                    return Login(result.Message);
                }
            }
            else
            {
                //访问失败
                return Login(result.Message);
            }

            //检查账户
            //包装数据
            JObject json = new JObject
            {
                { UserAccount.UserNameText, new JValue(fc["UserName"]) },
                { UserAccount.PasswordText, new JValue(fc["UserPassword"]) }
            };
            result = UserClient.Net_simplify_client.ReadFromServer(CommonLibrary.CommonHeadCode.SimplifyHeadCode.账户检查, json.ToString());
            if (result.IsSuccess)
            {
                //服务器应该返回账户的信息
                UserAccount account = JObject.Parse(result.Content).ToObject<UserAccount>();
                if (!account.LoginEnable)
                {
                    //不允许登录
                    return Login(account.ForbidMessage);
                }
                UserClient.UserAccount = account;
            }
            else
            {
                //访问失败
                return Login(result.Message);
            }


            result = UserClient.Net_simplify_client.ReadFromServer(CommonLibrary.CommonHeadCode.SimplifyHeadCode.更新检查);
            if (result.IsSuccess)
            {
                //服务器应该返回服务器的版本号
                SystemVersion sv = new SystemVersion(result.Content);
                //系统账户跳过低版本检测
                if (fc["UserName"] != "admin")
                {
                    if (UserClient.CurrentVersion != sv)
                    {
                        return Login("当前版本号不正确，需要服务器更新才允许登录。");
                    }
                }
                else
                {
                    if (UserClient.CurrentVersion < sv)
                    {
                        return Login("版本号过时，需要管理员更新才允许登录。");
                    }
                }
            }
            else
            {
                //访问失败
                return Login(result.Message);
            }

            //允许登录，并记录到Session
            return RedirectToAction("Index", "Home");
        }
    }
}