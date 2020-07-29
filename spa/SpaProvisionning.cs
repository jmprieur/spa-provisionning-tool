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
    class SpaProvisionning
    {
        static GraphServiceClient graphServiceClient;

        static IAuthenticationProvider authenticationProvider;

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
                case "update":
                    if (!string.IsNullOrWhiteSpace(options.WebRedirectUri))
                    {
                        await UpdateSpaWithWebRedirectUri(options);
                    }
                    else
                    {
                        await UpdateSpaWithSpaRedirectUri(options);
                    }
                    break;
                default:
                    break;
            }

        }

        private async Task UpdateSpaWithSpaRedirectUri(Options options)
        {
            string url = $"https://graph.microsoft.com/v1.0/applications?$filter=appId eq '{options.ClientId}'";

            HttpClient httpClient = new HttpClient();

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            await authenticationProvider.AuthenticateRequestAsync(httpRequestMessage);
            HttpResponseMessage httpReponseMessage = await httpClient.SendAsync(httpRequestMessage);
            string responseContent = await httpReponseMessage.Content.ReadAsStringAsync();
            if (httpReponseMessage.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.Error.WriteLine(responseContent);
                return;
            }

            dynamic apps = JsonConvert.DeserializeObject(responseContent);
            dynamic app = apps.value[0];
            app.web.implicitGrantSettings.enableAccessTokenIssuance = false;

            string id = app.id;
            string displayName = app.displayName;

            string body = Encoding.Default.GetString(Properties.Resources.spa_with_redirect_uri);
            body = body.Replace("[DisplayName]", displayName);
            body = body.Replace("[RedirectUri]", options.SpaRedirectUri);

            url = $"https://graph.microsoft.com/v1.0/applications/{id}";
            httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, url);
            httpRequestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
            await authenticationProvider.AuthenticateRequestAsync(httpRequestMessage);
            httpReponseMessage = await httpClient.SendAsync(httpRequestMessage);
            string secondResponseContent = await httpReponseMessage.Content.ReadAsStringAsync();
            if (httpReponseMessage.StatusCode != System.Net.HttpStatusCode.OK && httpReponseMessage.StatusCode!= System.Net.HttpStatusCode.NoContent)
            {
                Console.Error.WriteLine(secondResponseContent);
                return;
            }

            string appUrl = $"https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/{options.ClientId}/isMSAApp/";
            Console.WriteLine(appUrl);
        }

        private async Task UpdateSpaWithWebRedirectUri(Options options)
        {
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


        private async Task AddNewSpaWithSpaRedirectUri(Options options)
        {
            // We cannot the Graph SDK because spa is not there yet
            //
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
    }
}
