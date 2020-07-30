using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace spa
{
    public class Options
    {
        public DefaultAzureCredentialOptions DefaultAzureCredentialOptions { get; set; }

        public string SpaRedirectUri { get; set; }
        public string WebRedirectUri { get; set; }

        public string Action { get; set; }

        public string ClientId { get; set; }

        public string AppName { get; set; } = "New spa";

        public bool Validate(TextWriter textWriter)
        {
            bool validated = true;
            if (Action != "add" && Action != "updateToSpa" && Action != "updateToWeb")
            {
                textWriter.WriteLine("you need to specify an action (add or updateToSpa or updateToWeb)");
                validated = false;
            }

            if (Action == "add" && !(string.IsNullOrEmpty(SpaRedirectUri) ^ string.IsNullOrEmpty(WebRedirectUri)))
            {
                textWriter.WriteLine("you need to specify --spa-redirect-uri <redirectUri> or --web-redirect-uri <redirectUri>");
                validated = false;
            }

            if (string.IsNullOrEmpty(ClientId) && ((Action == "updateToSpa") || (Action == "updateToWeb")))
            {
                textWriter.WriteLine("updateToSpa or updateToWeb requires you to specify the --client-id");
                validated = false;
            }

            return validated;
        }


        /// <summary>
        /// Get the credentials from the command line
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Options GetOptionsFromConfiguration(string[] args)
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>
            {
                {"--tenant-id", "SharedTokenCacheTenantId"},
                {"--app-owner", "SharedTokenCacheUsername"},
                {"--client-id", "ClientId"},
                {"--spa-redirect-uri", "SpaRedirectUri"},
                {"--web-redirect-uri", "WebRedirectUri"},
                {"--app-name", "AppName"},
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddCommandLine(args, mapping);
            IConfiguration configuration = configurationBuilder.Build();
            DefaultAzureCredentialOptions defaultAzureCredentialOptions = ConfigurationBinder.Get<DefaultAzureCredentialOptions>(configuration);
            defaultAzureCredentialOptions.ExcludeManagedIdentityCredential = true;
            defaultAzureCredentialOptions.ExcludeInteractiveBrowserCredential = false;
            defaultAzureCredentialOptions.InteractiveBrowserTenantId = defaultAzureCredentialOptions.SharedTokenCacheTenantId;

            Options options = new Options();
            options.Action = args.FirstOrDefault(a => !a.StartsWith("--"));
            options.SpaRedirectUri = ConfigurationBinder.GetValue<string>(configuration, "SpaRedirectUri", null);
            options.WebRedirectUri = ConfigurationBinder.GetValue<string>(configuration, "WebRedirectUri", null);
            options.AppName = ConfigurationBinder.GetValue<string>(configuration, "AppName", null);
            options.ClientId = ConfigurationBinder.GetValue<string>(configuration, "ClientId", null);
            options.DefaultAzureCredentialOptions = defaultAzureCredentialOptions;
            return options;
        }
    }
}
