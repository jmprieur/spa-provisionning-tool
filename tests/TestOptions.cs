using NUnit.Framework;
using spa;
using System;

namespace tests
{
    /// SPA add "Name " --tenant-id<TenantId> --spa-redirect-uri https://localhost:12345
    /// > link to app registration
    /// <summary>
    /// SPA update --tenant-id <TenantId> -client-id <ClientID> --spa-redirect-uri https://localhost:12345
    /// > If redirect URL already exists, updates to SPA.Disable the implicit grant access token (ID Token*)
    /// SPA update --tenant-id<TenantId> -client-id<ClientID> --web-redirect-uri https://localhost:12345
    /// > If redirect URL already exists, updates to Web Enable the implicit grant access token and ID Token
    public class TestOptions
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void TestNewSpaWithSpaRedirectUri()
        {
            string[] args = { "add",
                              "--tenant-id", "msidentitysamplestesting.onmicrosoft.com",
                              "--spa-redirect-uri", "https://localhost:12345",
            };
            Options options = Options.GetOptionsFromConfiguration(args);
            Assert.AreEqual(options.Action, "add");
            Assert.AreEqual(options.SpaRedirectUri, "https://localhost:12345");
            Assert.IsNull(options.WebRedirectUri);
            Assert.IsTrue(options.Validate(Console.Error));
        }

        [Test]
        public void TestUpdateSpaWithSpaRedirectUri()
        {
            string[] args = { "update",
                              "--tenant-id", "msidentitysamplestesting.onmicrosoft.com",
                              "--spa-redirect-uri", "https://localhost:12345",
            };
            Options options = Options.GetOptionsFromConfiguration(args);
            Assert.AreEqual(options.Action, "update");
            Assert.AreEqual(options.SpaRedirectUri, "https://localhost:12345");
            Assert.IsNull(options.WebRedirectUri);
            Assert.IsTrue(options.Validate(Console.Error));
        }

        [Test]
        public void TestNewSpaWithWebRedirectUri()
        {
            string[] args = { "add",
                              "--tenant-id", "msidentitysamplestesting.onmicrosoft.com",
                              "--web-redirect-uri", "https://localhost:54321"
            };
            Options options = Options.GetOptionsFromConfiguration(args);
            Assert.AreEqual(options.Action, "add");
            Assert.AreEqual(options.WebRedirectUri, "https://localhost:54321");
            Assert.IsNull(options.SpaRedirectUri);
            Assert.IsTrue(options.Validate(Console.Error));
        }

        [Test]
        public void TestNewSpaWithSpaAndWebRedirectUri()
        {
            string[] args = { "add",
                              "--tenant-id", "msidentitysamplestesting.onmicrosoft.com",
                              "--spa-redirect-uri", "https://localhost:12345",
                              "--web-redirect-uri", "https://localhost:54321"
            };
            Options options = Options.GetOptionsFromConfiguration(args);
            Assert.IsFalse(options.Validate(Console.Error));
        }
    }
}