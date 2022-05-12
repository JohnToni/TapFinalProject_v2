using System;
using System.Linq;
using TAP21_22_AuctionSite.Interface;
using NUnit.Framework;
using ScazzolaImplementation;
using TAP21_22.AuctionSite.Testing;


namespace ScazzolaTest
{
    public class Constraint
    {
        public static string ConnectionString = "Data Source=.;Initial Catalog=ScazzolaLocalhost;Integrated Security=True;";
        public static string siteName = "site_1";
        
        public static HostFactoryObject SetUpHostFactory() => new HostFactoryObject();
        public static HostObject setUpHost(HostFactoryObject hostFactory)
        {
            hostFactory.CreateHost(ConnectionString);
            return (HostObject)hostFactory.LoadHost(ConnectionString, new TestAlarmClockFactory());
        }
        public static SiteObject SetUpSite(HostObject host,string name, int tz, int seis, double mbi)
        {
            host.CreateSite(siteName, tz, seis, mbi);
            return (SiteObject)host.LoadSite(siteName);
        }
    }

    [TestFixture]
    public class SiteTests
    {
        public static HostFactoryObject hostFactory;
        public static HostObject host;
        [SetUp]
        public void Setup()
        {
            hostFactory = Constraint.SetUpHostFactory();
            host = Constraint.setUpHost(hostFactory);
        }

        [Test]
        public void CreateNonNullSite()
        {
            var site = Constraint.SetUpSite(host, Constraint.siteName, 2, 3, 4);
            //Assert.That(()=>site.Name,Is.EqualTo(Constraint.siteName));
            //Assert.That(()=>host.GetSiteInfos().ToList(),Is.Not.Empty);
            Assert.That(()=>host.GetSiteInfos().Single().Name,Is.EqualTo(Constraint.siteName));
        }
        [Test]
        public void CreateAlreadyExistingSite()
        {
            var site = Constraint.SetUpSite(host, Constraint.siteName, 2, 3, 4);
            Assert.That(()=>Constraint.SetUpSite(host,Constraint.siteName,1,2,3),Throws.TypeOf<AuctionSiteNameAlreadyInUseException>());
        }
    }

    [TestFixture]
    public class HostTests
    {
        public static string ConnectionString = "localhost";

        [Test]
        public void CreateNonNullHost()
        {
            HostFactoryObject hostF = new HostFactoryObject();
            hostF.CreateHost(ConnectionString);

            var host = hostF.LoadHost(ConnectionString, new TestAlarmClockFactory());
            Assert.That(() => host, Is.Not.Null);

        }
    }
}
