using System;
using System.Threading.Tasks;

namespace spa
{
    class Program
    {
        /// SPA add --app-Name "Name " --tenant-id<TenantId> --spa-redirect-uri https://localhost:12345
        /// > link to app registration
        /// <summary>
        /// SPA update --tenant-id <TenantId> -client-id <ClientID> --spa-redirect-uri https://localhost:12345
        /// > If redirect URL already exists, updates to SPA.Disable the implicit grant access token (ID Token*)
        /// SPA update --tenant-id<TenantId> -client-id<ClientID> --web-redirect-uri https://localhost:12345
        /// > If redirect URL already exists, updates to Web Enable the implicit grant access token and ID Token
        /// </summary>
        /// <param name="args"></param>
        static async Task Main(string[] args)
        {
            Options options = Options.GetOptionsFromConfiguration(args);
            if (options.Validate(Console.Error))
            {
                SpaProvisionning spaProvisionning = new SpaProvisionning();
                await spaProvisionning.Provision(options);
            }
        }


    }
}
