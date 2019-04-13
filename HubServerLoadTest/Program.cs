﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RuPengMessageHub.NetSDK;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HubServerLoadTest
{
    class Program
    {
        static string appkey;
        static string appsecret;
        static IHttpClientFactory httpClientFactory;
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

            var builder = new ConfigurationBuilder()
                .AddCommandLine(args);
            var configuration = builder.Build();
            appkey = configuration["appkey"];
            appsecret = configuration["appsecret"];
            List<Task> tasks = new List<Task>();
            for(int i=0;i<500;i++)
            {
                tasks.Add(StartOneClientAsync(i));
                Console.WriteLine("任务启动"+i);
            }
;           Task.WaitAll(tasks.ToArray());

            Console.WriteLine("ok!");
            Console.ReadKey();
        }

        static async Task StartOneClientAsync(int taskId)
        {
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJBcHBLZXkiOiJydXBlbmdnb25na2Fpa2UiLCJEaXNwbGF5TmFtZSI6IuadqOS4reenkSIsIlVzZXJJZCI6IjI4IiwiZXhwIjoxNTU3NjcxMDI5LCJpc3MiOiJtZSIsImF1ZCI6InlvdSJ9.Xa9hmoMbCRzwIyGvnUX7L4GSAXhVPUsi2jLz4Kvj2mk";
            var chatRoomId = "Activity_72";


            var connection = new HubConnectionBuilder()
                            .WithUrl("https://msghub.rupeng.com/messageHub", options =>
                            {
                                options.AccessTokenProvider = () => Task.FromResult(token);
                            })
                            .Build();

            connection.Closed += async (error) =>
            {
                Console.WriteLine("连接断开，尝试重连");
                await Task.Delay(2000);
                await connection.StartAsync();
            };

            try
            {
                await connection.StartAsync();
                await connection.InvokeAsync("GetGroupMessages", chatRoomId);
                MsgHubClient client = new MsgHubClient("https://msghub.rupeng.com/", httpClientFactory);
                string sdkToken = await client.GetTokenAsync("28", "yzk", appkey, DateTime.Now.ToFileTime(),appsecret);
                Random rand = new Random();
                for (; ; )
                {
                    try
                    {
                        await client.SendGroupMessageAsync(token, chatRoomId, "txtMsg","["+Dns.GetHostName()+"]-"+Guid.NewGuid());
                        Console.WriteLine("发送消息完成,taskId=" + taskId);
                        await Task.Delay(rand.Next(2000, 5000));//不能用Thread.Sleep
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("发送消息失败, taskId = " + taskId + "," + ex);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"启动任务失败，taskId=${taskId},${ex}");
            }

        }

    }
}
