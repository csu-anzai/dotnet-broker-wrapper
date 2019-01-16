﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrokerFacade.Util
{
    public class SubscriptionHostname
    {
        private static Random random = new Random();

        public static string GetUniqueSubscription()
        {
            var hostname = Environment.GetEnvironmentVariable("HOSTNAME");
            if(hostname == null)
            {
                hostname = Environment.GetEnvironmentVariable("COMPUTERNAME");
            }
            if (hostname != null)
            {
                var envName = Environment.GetEnvironmentVariable("ENVIRONMENT_NAME");
                hostname = hostname.Replace(envName + "-", "");
                hostname = hostname.Replace("-", "_");
                return hostname;
            }
            else
            {
                return GetRandomString();
            }
        }

        private static string GetRandomString()
        {
            var randomString = RandomString(8);
            return randomString;
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}