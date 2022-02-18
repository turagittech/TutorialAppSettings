using System;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;


// works with dotnet 5.0
namespace TutorialAppSettings

{
    static class Program
    {
        //Declaring the configuration
        private static IConfiguration _configuration = null;

        static void Main(string[] args)
        {
            // So you know it's working
            Console.WriteLine("Hello World!");
            // Run the configuration builder
            BuildConfig(args);
            //Grab the returned value associated with the Secret Name 
            //S So we dont expose the name of our secret in the key vault for added security
            //and help you understand how you can manipulate hiding things
            //This hidden value can be added in the variables in Azure Devops when you run the built code for deployment
            // These two lines could be condensed, for you to try
            var dbConnectSecret = _configuration["SecretName"];
            var sqlConnectionStr = _configuration[dbConnectSecret];
            
            // Outputs are to show you and help you with debugging the behaviour while learning.
            // Could be writing to a log in production usage 
            Console.WriteLine("Data Vault Secret String : " + sqlConnectionStr);
            try
            // Due to the flakiness of network and db connections using a try catch
            //One productive use of this is database building using fluentmigrator or DbUp 
            // call the fluentmigrator code in this using block
            {
                using (SqlConnection dbconnection = new SqlConnection(_configuration[dbConnectSecret]))
                {
                    dbconnection.Open();
                    String sqlQuery = "SELECT name, collation_name FROM sys.databases";

                    using (SqlCommand command = new SqlCommand(sqlQuery, dbconnection))
                    {
                        using (SqlDataReader dbreader = command.ExecuteReader())
                        {
                            while (dbreader.Read())
                            {
                                Console.WriteLine("{0} {1}", dbreader.GetString(0), dbreader.GetString(1));
                                
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nDone. Press enter.");
            Console.ReadLine();
            
        }

        private static void BuildConfig(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                // the <Program> stops you having to add your user secrets ID
                // meaning the code wont need updating with the key when a new dev works on it
                .AddCommandLine(args);

            var builtConfig = configurationBuilder.Build();

            var keyVaultName = builtConfig["KeyVaultName"];
            var secretName = builtConfig["SecretName"];
            Console.WriteLine(keyVaultName);
            Console.WriteLine(secretName);
            if (!string.IsNullOrEmpty(keyVaultName))
            {
                var secretClient = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"),
                    new DefaultAzureCredential());
                configurationBuilder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                var secret = secretClient.GetSecret(secretName);
                Console.WriteLine(secret);
            }

            _configuration = configurationBuilder.Build();
        }
    }
}