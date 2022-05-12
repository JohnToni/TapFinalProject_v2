using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using TAP21_22.AlarmClock.Interface;
using TAP21_22_AuctionSite.Interface;

namespace Interfaces
{
    public class AuctionSiteException : Exception { }
    public class AuctionSiteArgumentException : AuctionSiteException
    {
        public virtual string? paramName { get; }
        public AuctionSiteArgumentException() { }
        public AuctionSiteArgumentException(string message) { }
        public AuctionSiteArgumentException(string message, Exception inner) { }
        public AuctionSiteArgumentException(string message, string? paramName) { }
        public AuctionSiteArgumentException(string message, string? paramName, Exception inner) { }
        protected AuctionSiteArgumentException(SerializationInfo info, StreamingContext context) { }
    }
    public class AuctionSiteArgumentNullException : AuctionSiteArgumentException
    {
        public virtual string? paramName { get; }
        public AuctionSiteArgumentNullException() { }
        public AuctionSiteArgumentNullException(string message) { }
        public AuctionSiteArgumentNullException(string message, Exception inner) { }
        public AuctionSiteArgumentNullException(string message, string? paramName) { }
        public AuctionSiteArgumentNullException(string message, string? paramName, Exception inner) { }
        protected AuctionSiteArgumentNullException(SerializationInfo info, StreamingContext context) { }
    }

    public class AuctionSiteArgumentOutOfRangeException : AuctionSiteArgumentException
    {
        public AuctionSiteArgumentOutOfRangeException() { }
        public AuctionSiteArgumentOutOfRangeException(string message){}
    }
    public class AuctionSiteConcurrentChangeException : AuctionSiteException { }
    public class AuctionSiteInexistentNameException : AuctionSiteException { }

    public class AuctionSiteNameAlreadyInUseException : AuctionSiteException
    {
        public AuctionSiteNameAlreadyInUseException(){ }
        public AuctionSiteNameAlreadyInUseException(string message) { }
    }
    public class AuctionSiteUnavailableDbException : AuctionSiteException { }

    public class AuctionSiteUnavailableTimeMachineException : AuctionSiteException
    {
        public AuctionSiteUnavailableTimeMachineException(){ }
        public AuctionSiteUnavailableTimeMachineException(string message) { }
    }

    public class AuctionSiteInvalidOperationException : AuctionSiteException
    {
        public AuctionSiteInvalidOperationException(){}
        public AuctionSiteInvalidOperationException(string message) {}
        public AuctionSiteInvalidOperationException(string message, Exception inner) { }

    }

    public class DomainConstraints
    {
        public const int MinSiteName = 1;
        public const int MaxSiteName = 128;
        public const int MinUserName = 3;
        public const int MaxUserName = 64;
        public const int MinUserPassword = 4;
        public const int MinTimeZone = -12;
        public const int MaxTimeZone = 12;
    }
    public interface IHostFactory
    {
        public void CreateHost(string connectionString);
        public IHost LoadHost(string connectionString, IAlarmClockFactory alarmClockFactory);
    }

    public interface IHost
    {
        public void CreateSite(string name, int timezone, int sessionExpirationTimeInSeconds,
            double minimumBidIncrement);

        public IEnumerable<(string Name, int TimeZone)> GetSiteInfos();
        public ISite LoadSite(string name);
    }

    public interface ISite
    {
        public string Name { get; }
        public int Timezone { get; }
        public int SessionExpirationInSeconds { get; }
        public double MinimumBidIncrement { get; }
        public IEnumerable<IUser> ToyGetUsers();
        public IEnumerable<ISession> ToyGetSessions();
        public IEnumerable<IAuction> ToyGetAuctions(bool onlyNotEnded);
        public ISession? Login(string username, string password);
        public void CreateUser(string username, string password);
        public void Delete();
        public DateTime Now();
    }

    public interface ISession
    {
        public string Id { get; }
        public DateTime ValidUntil { get; }
        public IUser User { get; }
        public void Logout();
        public IAuction CreateAuction(string description, DateTime endsOn, double startingPrice);
    }
    public interface IUser
    {
        public string Username { get; }
        public IEnumerable<IAuction> WonAuctions();
        public void Delete();
    }

    public interface IAuction
    {
        public int Id { get; }
        public IUser Seller { get; }
        public string Description { get; }
        public DateTime EndsOn { get; }
        public IUser? CurrentWinner();
        public double CurrentPrice();
        public void Delete();
        public bool Bid(ISession session, double offer);
    }

    public class TapDbContext : DbContext
    {
        public int NumberOfCreatedContext { get; set; } = 0;
        public TapDbContext() {}
        public TapDbContext(string connectionString)
            : base(new DbContextOptionsBuilder<TapDbContext>()
                .UseSqlServer(connectionString)
                .Options)
        { }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);
            options.UseSqlServer(@"Data Source=.;Initial Catalog=TAP_PROJ;Integrated Security=True;");
            //options.LogTo(Console.WriteLine).EnableSensitiveDataLogging();
        }
    }
}
