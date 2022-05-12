using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TAP21_22_AuctionSite.Interface;

namespace ScazzolaImplementation
{
    public class ToyDbContext : TapDbContext
    {
        public ToyDbContext(string connectionString)
        : base(new DbContextOptionsBuilder<ToyDbContext>()
        .UseSqlServer(connectionString)
        .Options)
        { NumberOfCreatedContexts++; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);
            options.UseSqlServer(@"Data Source=.;Initial Catalog=TAP_PROJ;Integrated Security=True;");
            options.LogTo(Console.WriteLine).EnableSensitiveDataLogging();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //HOST
            var siteEntity = modelBuilder.Entity<DbSite>();
            siteEntity.HasOne(s => s.Host).WithMany(h => h.ActiveSites).OnDelete(DeleteBehavior.ClientCascade);

            //modelBuilder.Entity<DbAuction>().HasOne(a => a.Seller).WithMany(u => u.ActiveAuctions)
            //.HasForeignKey(k => k.SellerId);

            //SITE
            //modelBuilder.Entity<DbSite>().HasMany(s => s.ActiveSessions);
            //modelBuilder.Entity<DbSite>().HasMany(s => s.ActiveUsers);
            //SESSION
            //modelBuilder.Entity<DbSession>().HasMany(s => s.ActiveAuctions);


            //modelBuilder.Entity<SiteObject>().HasMany(u => u.ActiveUsers).WithMany("ActiveUsers").UsingEntity(pp => pp.ToTable("SiteActiveUsers"));
            //modelBuilder.Entity<SiteObject>().HasMany(s => s.ActiveSessions).WithMany("ActiveSessions").UsingEntity(pp => pp.ToTable("SiteActiveSessions"));
            //modelBuilder.Entity<SessionObject>().HasMany(a => a.ActiveAuctions).WithMany("ActiveAuctions").UsingEntity(pp => pp.ToTable("SessionActiveAuctions"));
        }


        public class DbHost
        {
            [Key] public int HostId { get; set; }
            public string ConnectionString { get; set; }

            /*CHIAVI ESTERNE*/
            public virtual ICollection<DbSite> ActiveSites { get; set; } = new List<DbSite>();

            public DbHost(string connectionString)
            {
                ConnectionString = connectionString;
            }
        }

        public class DbSite
        {
            [Key] public int SiteId { get; set; }
            public string Name { get; set; }
            public int Timezone { get; set; }
            public int SessionExpirationInSeconds { get; set; }
            public double MinimumBidIncrement { get; set; }
            public List<DbUser> ActiveUsers { get; set; } = new List<DbUser>();
            public List<DbSession> ActiveSessions { get; set; } = new List<DbSession>();

            /*CHIAVI ESTERNE*/
            public DbHost Host { get; set; }
            //public int HostId { get; set; }

            public DbSite(string name, int timezone, int sessionExpirationInSeconds, double minimumBidIncrement)
            {
                Name = name;
                Timezone = timezone;
                SessionExpirationInSeconds = sessionExpirationInSeconds;
                MinimumBidIncrement = minimumBidIncrement;
            }
        }

        public class DbSession
        {
            public string Id { get; set; }
            public DateTime ValidUntil { get; set; }
            public DbUser User { get; set; }
            public double MinimumBidIncrement { get; set; }
            public List<DbAuction> ActiveAuctions { get; set; } = new List<DbAuction>();

            public DbSession() { }
            public DbSession(DateTime validUntil, DbUser user, double minimumBidIncrement)
            {
                ValidUntil = validUntil;
                User = user;
                MinimumBidIncrement = minimumBidIncrement;
            }
        }

        public class DbUser
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }

            public ICollection<DbAuction> ActiveAuctions { get; set; } = new List<DbAuction>();
            public DbUser(string username, string password)
            {
                Username = username;
                Password = password;
            }
        }
        public class DbAuction
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public DateTime EndsOn { get; set; }
            public double MinimumBidIncrement { get; set; }
            public double StartingPrice { get; set; }
            public bool Ended { get; set; } = false;

            public int SellerId { get; set; }
            public DbUser Seller { get; set; }

            //public Dictionary<string, double> activeBids { get; set; } 

            public DbAuction() { }
            public DbAuction(DbUser seller, string description, DateTime endsOn, double minumumBidIncrement, double startingPrice)
            {
                Seller = seller;
                Description = description;
                EndsOn = endsOn;
                MinimumBidIncrement = minumumBidIncrement;
                StartingPrice = startingPrice;
            }
        }

        public DbSet<DbHost> Hosts { get; set; }
        public DbSet<DbSite> Sites { get; set; }
        public DbSet<DbSession> Sessions { get; set; }
        public DbSet<DbAuction> Auctions { get; set; }
        public DbSet<DbUser> Users { get; set; }
    }
}
