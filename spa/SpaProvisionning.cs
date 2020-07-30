using Azure.Identity;
using Microsoft.Graph;
using Newtonsoft.Json;
using ProvisionAadApp;
using System;
using System.Net.Http;

using System.Text;
using System.Threading.Tasks;

namespace spa
{
    /// <summary>
    /// Provisionning for SPA applications
    /// </summary>
    class SpaProvisionning
    {
        /// <summary>
        /// Graph SDK service client (used only in 1 case out of 4 as it does not support
        /// the strongly type Spa yet
        /// </summary>
        static GraphServiceClient graphServiceClient;

        /// <summary>
        /// Authentication provider
        /// </summary>
        static IAuthenticationProvider authenticationProvider;

        /// <summary>
        /// Http Client
        /// </summary>
        static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Provisions a SPA application
        /// </summary>
        /// <param name="options">Command line options</param>
        /// <returns></returns>
        internal async Task Provision(Options options)
        {
            DefaultAzureCredential credential = new DefaultAzureCredential(options.DefaultAzureCredentialOptions);
            authenticationProvider = new TokenCredentialCredentialProvider(credential,
                                                            new string[] { "Application.ReadWrite.All" });
            if (graphServiceClient == null)
            {
                graphServiceClient = new GraphServiceClient(authenticationProvider);
            }

            switch (options.Action)
            {
                case "add":
                    if (!string.IsNullOrWhiteSpace(options.WebRedirectUri))
                    {
                        await AddNewSpaWithWebRedirectUri(options);
                    }
                    else
                    {
                        await AddNewSpaWithSpaRedirectUri(options);
                    }
                    break;
                case "updateToSpa":
                    await UpdateSpaAppWithSpaRedirectUri(options);
                    break;
                case "updateToWeb":
                    await UpdateSpaAppWithWebRedirectUri(options);
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// Update an app to be a SPA (with SPA redirect URIs)
        /// </summary>
        /// <param name="options">Command line options</param>
        /// <returns></returns>
        private async Task UpdateSpaAppWithSpaRedirectUri(Options options)
        {
            // Read the application
            dynamic app = ReadApplicationRegistration(options);
            if (app != null)
            {
                // Is it already a Spa?
                var spaRedirectUris = app.spa?.redirectUris;
                if (spaRedirectUris != null && spaRedirectUris.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Application '{app.appName}' is already a SPA");
                    Console.ResetColor();
                }
                else
                {
                    // The Web redirect URIs will be transformed into SPA redirect URIs
                    string webredirectUris = JsonConvert.SerializeObject(app.web.redirectUris);

                    // A SPA (MSAL v2.0) uses Auth Code Flow, and not implicit flow
                    app.web.implicitGrantSettings.enableAccessTokenIssuance = false;
                    string implicitGrantSettings = JsonConvert.SerializeObject(app.web.implicitGrantSettings);

                    // Patch content
                    string body = "{ \"spa\" : { \"redirectUris\" : " + webredirectUris + " }, " +
                                    "\"web\" : { \"implicitGrantSettings\" : " + implicitGrantSettings + " } }";
                    await UpdateApplicationRegistration(app.id, body);
                }
                WriteUrlOfAppInPortal(options);
            }
        }

        /// <summary>
        /// Update an app to be a web application (with Web redirect URIs)
        /// </summary>
        /// <param name="options">Command line options</param>
        /// <returns></returns>
        private async Task UpdateSpaAppWithWebRedirectUri(Options options)
        {
            dynamic app = ReadApplicationRegistration(options);
            if (app != null)
            {
                // Is it already a Web app?
                var webRedirectUris = app.web?.redirectUris;
                if (webRedirectUris != null && webRedirectUris.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Application '{app.appName}' is already a Web app");
                    Console.ResetColor();
                }
                else
                {
                    // The SPA redirect Uris are transformed into Web redirect URIs
                    string spaRedirectUris = JsonConvert.SerializeObject(app.spa.redirectUris);

                    // SPAs v1.0 with Web redirect URIs use implicity flow
                    app.web.implicitGrantSettings.enableAccessTokenIssuance = true;
                    app.web.implicitGrantSettings.enableIdTokenIssuance = true;
                    string implicitGrantSettings = JsonConvert.SerializeObject(app.web.implicitGrantSettings);

                    string body = "{ \"web\" : { \"redirectUris\" : " + spaRedirectUris + ", " +
                                    "            \"implicitGrantSettings\" : " + implicitGrantSettings + " } }";

                    await UpdateApplicationRegistration(app.id, body);
                }
                WriteUrlOfAppInPortal(options);
            }
        }

        /// <summary>
        /// Register a new SPA application with a web redirect URI
        /// </summary>
        /// <param name="options">Command line options</param>
        /// <returns></returns>
        private async Task AddNewSpaWithWebRedirectUri(Options options)
        {
            Application application;

            if (!string.IsNullOrWhiteSpace(options.WebRedirectUri))
            {
                application = new Application()
                {
                    DisplayName = options.AppName,
                    SignInAudience = "AzureADandPersonalMicrosoftAccount",
                    Tags = new[] { "{WindowsAzureActiveDirectoryIntegratedApp}" },
                    Web = new WebApplication()
                    {
                        ImplicitGrantSettings = new ImplicitGrantSettings
                        {
                            EnableIdTokenIssuance = true,
                            EnableAccessTokenIssuance = true
                        },
                        RedirectUris = new string[] { options.WebRedirectUri },
                    },
                };
                var app = await graphServiceClient.Applications
                       .Request()
                       .AddAsync(application);

                string appUrl = $"https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/{app.AppId}/isMSAApp/";
                Console.WriteLine(appUrl);
            }
        }


        /// <summary>
        /// Register a new SPA with SPA redirect URIs
        /// </summary>
        /// <param name="options">Command line options</param>
        /// <returns></returns>
        private async Task AddNewSpaWithSpaRedirectUri(Options options)
        {
            // We cannot the Graph SDK because spa is not available yet in the object
            // model of the Graph SDK
            string url = "https://graph.microsoft.com/v1.0/applications";
            string body = Encoding.Default.GetString(Properties.Resources.spa_with_redirect_uri);
            body = body.Replace("[DisplayName]", options.AppName);
            body = body.Replace("[RedirectUri]", options.SpaRedirectUri);

            HttpClient httpClient = new HttpClient();
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
            await authenticationProvider.AuthenticateRequestAsync(httpRequestMessage);
            HttpResponseMessage httpReponseMessage = await httpClient.SendAsync(httpRequestMessage);
            string responseContent = await httpReponseMessage.Content.ReadAsStringAsync();

            string appId = null;
            if (httpReponseMessage.StatusCode == System.Net.HttpStatusCode.Created)
            {
                dynamic app = JsonConvert.DeserializeObject(responseContent);
                appId = app.appId;
            }

            string appUrl = $"https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/{appId}/isMSAApp/";
            Console.WriteLine(appUrl);
        }


        /// <summary>
        /// Read the application registration
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task<dynamic> ReadApplicationRegistration(Options options)
        {
            string url = $"https://graph.microsoft.com/v1.0/applications?$filter=appId eq '{options.ClientId}'";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            await authenticationProvider.AuthenticateRequestAsync(httpRequestMessage);
            HttpResponseMessage httpReponseMessage = await httpClient.SendAsync(httpRequestMessage);
            string responseContent = await httpReponseMessage.Content.ReadAsStringAsync();
            if (httpReponseMessage.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(responseContent);
                Console.ResetColor();
                return null;
            }

            dynamic apps = JsonConvert.DeserializeObject(responseContent);
            dynamic app = apps.value[0];
            return app;
        }

        /// <summary>
        /// Update the application registration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        private async Task UpdateApplicationRegistration(string id, string body)
        {
            string url = $"https://graph.microsoft.com/v1.0/applications/{id}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, url);
            httpRequestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
            await authenticationProvider.AuthenticateRequestAsync(httpRequestMessage);
            HttpResponseMessage httpReponseMessage = await httpClient.SendAsync(httpRequestMessage);
            string secondResponseContent = await httpReponseMessage.Content.ReadAsStringAsync();
            if (httpReponseMessage.StatusCode != System.Net.HttpStatusCode.OK && httpReponseMessage.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                Console.Error.WriteLine(secondResponseContent);
            }
        }

        private static void WriteUrlOfAppInPortal(Options options)
        {
            string appUrl = $"https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/{options.ClientId}/isMSAApp/";
            Console.WriteLine(appUrl);
        }
    }
}
