using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework.Constraints;
using TAP21_22.AlarmClock.Interface;
using TAP21_22.AuctionSite.Testing;
using TAP21_22_AuctionSite.Interface;
using AuctionSiteArgumentException = TAP21_22_AuctionSite.Interface.AuctionSiteArgumentException;
using AuctionSiteArgumentNullException = TAP21_22_AuctionSite.Interface.AuctionSiteArgumentNullException;
using AuctionSiteArgumentOutOfRangeException = Interfaces.AuctionSiteArgumentOutOfRangeException;
using AuctionSiteInvalidOperationException = TAP21_22_AuctionSite.Interface.AuctionSiteInvalidOperationException;
using AuctionSiteNameAlreadyInUseException = TAP21_22_AuctionSite.Interface.AuctionSiteNameAlreadyInUseException;
using AuctionSiteUnavailableDbException = Interfaces.AuctionSiteUnavailableDbException;
using AuctionSiteUnavailableTimeMachineException = Interfaces.AuctionSiteUnavailableTimeMachineException;
using DomainConstraints = TAP21_22_AuctionSite.Interface.DomainConstraints;
using IAuction = TAP21_22_AuctionSite.Interface.IAuction;
using IHost = TAP21_22_AuctionSite.Interface.IHost;
using IHostFactory = TAP21_22_AuctionSite.Interface.IHostFactory;
using ISession = TAP21_22_AuctionSite.Interface.ISession;
using ISite = TAP21_22_AuctionSite.Interface.ISite;
using IUser = TAP21_22_AuctionSite.Interface.IUser;
using TapDbContext = TAP21_22_AuctionSite.Interface.TapDbContext;

namespace ScazzolaImplementation
{
    public class ConnectionString
    {
        public static string value;
    }

    public class HostFactoryObject : IHostFactory
    {
        public void CreateHost(string connectionString)
        {
            if (connectionString == null)
                throw new AuctionSiteArgumentNullException("connection string cannot be null");
            if (connectionString == "") throw new AuctionSiteArgumentException("connection string cannot be empty");

            using (var c = new ToyDbContext(connectionString))
            {
                c.Database.EnsureDeleted();
                c.Database.EnsureCreated();

                ConnectionString.value = connectionString;

                c.Hosts.Add(new ToyDbContext.DbHost(connectionString));
                c.SaveChanges();
            }
        }

        public IHost LoadHost(string connectionString, IAlarmClockFactory alarmClockFactory)
        {
            if (connectionString == null)
                throw new AuctionSiteArgumentNullException("connection string cannot be null");
            if (connectionString == "") throw new AuctionSiteArgumentNullException("connection string cannot be empty");
            if (alarmClockFactory == null)
                throw new AuctionSiteArgumentNullException("alarmClockFactory cannot be null");


            using (var c = new ToyDbContext(connectionString))
            {
                if (!c.Hosts.Any())
                    throw new AuctionSiteArgumentNullException(
                        $"Cannot find host with connection string={connectionString}.");
            }

            return new HostObject(alarmClockFactory);
        }
    }

    public class HostObject : IHost
    {
        public IAlarmClockFactory AlarmClockFactory { get; set; }

        public HostObject(IAlarmClockFactory alarmClockFactory)
        {
            AlarmClockFactory = alarmClockFactory;
        }

        public void CreateSite(string name, int timezone, int sessionExpirationTimeInSeconds,
            double minimumBidIncrement)
        {
            if (name == null) throw new AuctionSiteArgumentException("name cannot be null");
            if (name == "") throw new AuctionSiteArgumentException("Name cannot be empty");
            if (name.Length < DomainConstraints.MinSiteName)
                throw new AuctionSiteArgumentException("Site name too short");
            if (name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException("Site name too long");

            if (timezone < DomainConstraints.MinTimeZone)
                throw new AuctionSiteArgumentOutOfRangeException("Timezone too low");
            if (timezone > DomainConstraints.MaxTimeZone)
                throw new AuctionSiteArgumentOutOfRangeException("Timezone too high");

            if (sessionExpirationTimeInSeconds < 0)
                throw new AuctionSiteArgumentNullException("Session Expire must be positive");
            if (minimumBidIncrement < 0)
                throw new AuctionSiteArgumentNullException("Minimum Bid Increment number must be positive");

            using (var c = new ToyDbContext(ConnectionString.value))
            {
                if (c.Sites.Any(s => s.Name == name))
                    throw new AuctionSiteNameAlreadyInUseException("The site already exists!");

                var newSite =
                    new ToyDbContext.DbSite(name, timezone, sessionExpirationTimeInSeconds, minimumBidIncrement);
                var dbHost = c.Hosts.First();
                
                dbHost.ActiveSites.Add(newSite);
                c.Sites.Add(newSite);
                c.SaveChanges();
            }
        }

        public IEnumerable<(string Name, int TimeZone)> GetSiteInfos()
        {
            using (var c = new ToyDbContext(ConnectionString.value))
            {
                var host = c.Hosts.First();
                var siteList = host.ActiveSites.ToList();
                
                foreach (var site in siteList)
                {
                    yield return (site.Name, site.Timezone);
                }
            }
        }

        public ISite LoadSite(string name)
        {
            //TODO check if database is not available???

            if (name == null) throw new AuctionSiteArgumentNullException("name cannot be null");
            if (name == "") throw new AuctionSiteArgumentException("name cannot be empty");
            if (name.Length < DomainConstraints.MinSiteName || name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException("Site name too long/short");

            try
            {
                using (var c = new ToyDbContext(ConnectionString.value))
                {
                    var site = c.Sites.Single(s => s.Name == name);

                    return new SiteObject(name,
                        AlarmClockFactory.InstantiateAlarmClock(site.Timezone),
                        site.SessionExpirationInSeconds,
                        site.MinimumBidIncrement);
                }
            }
            catch (Exception e)
            {
                throw new AuctionSiteArgumentNullException("Site not found", e);
            }
        }
    }

    public class SiteObject : ISite
    {
        public string Name { get; }
        public int Timezone { get; }
        public int SessionExpirationInSeconds { get; }
        public double MinimumBidIncrement { get; }
        public IAlarmClock AlarmClock { get; }

        public SiteObject(string name, IAlarmClock alarmClock, int sessionExpirationInSeconds,
            double minimumBidIncrement)
        {
            Name = name;
            AlarmClock = alarmClock;
            Timezone = alarmClock.Timezone;
            SessionExpirationInSeconds = sessionExpirationInSeconds;
            MinimumBidIncrement = minimumBidIncrement;
        }

        public IEnumerable<IUser> ToyGetUsers()
        {
            using (var c = new ToyDbContext(ConnectionString.value))
            {
                var site = c.Sites.Single(s => s.Name == Name);
                foreach (var user in site.ActiveUsers)
                {
                    yield return new UserObject(user.Username, user.Password);
                }
            }
        }

        public IEnumerable<ISession> ToyGetSessions()
        {
            using (var c = new ToyDbContext(ConnectionString.value))
            {
                var site = c.Sites.Single(s => s.Name == Name);
                foreach (var session in site.ActiveSessions)
                {
                    yield return new SessionObject(session.ValidUntil, session.MinimumBidIncrement, new UserObject());
                }
            }
        }

        public IEnumerable<IAuction> ToyGetAuctions(bool onlyNotEnded)
        {
            /*
            foreach (var session in ActiveSessions)
            {
                foreach (var auction in session.ActiveAuctions)
                {
                    yield return auction;
                }
            }
            */
            throw new NotImplementedException();
        }

        public ISession? Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
                throw new Interfaces.AuctionSiteArgumentNullException("Username cannot be null or empty.");
            if (string.IsNullOrEmpty(password))
                throw new Interfaces.AuctionSiteArgumentNullException("Password cannot be null or empty.");
            if (username.Length > DomainConstraints.MaxUserName)
                throw new AuctionSiteArgumentException("Username is too long.");
            if (username.Length < DomainConstraints.MinUserName)
                throw new AuctionSiteArgumentException("Username is too short.");

            using (var c = new ToyDbContext(ConnectionString.value))
            {
                ToyDbContext.DbUser user;

                try
                {
                    user = c.Users.Single(u => u.Username == username);
                }
                catch (ArgumentNullException e)
                {
                    throw new AuctionSiteArgumentNullException("User not found.", e);
                }

                if (c.Sessions.Any(s => s.User.Username == username))
                    return null;

                var newSession = new ToyDbContext.DbSession(Now().AddHours(1), user, MinimumBidIncrement);
                c.Sessions.Add(newSession);
                //Active sessions??

                return new SessionObject(Now().AddHours(1), MinimumBidIncrement,
                    new UserObject(user.Username, user.Password));
            }
        }

        public void CreateUser(string username, string password)
        {
            using (var c = new ToyDbContext(ConnectionString.value))
            {
                if (c.Users.Any(u => u.Username == username))
                    throw new AuctionSiteArgumentNullException("User already exists");

                var site = c.Sites.SingleOrDefault(s => s.Name == Name);
                if (site == default) throw new AuctionSiteArgumentNullException("Site not found");

                var newUser = new ToyDbContext.DbUser(username, password);

                c.Users.Add(newUser);
                site.ActiveUsers.Add(newUser);

                c.SaveChanges();
            }

            //ActiveUsers.Add(new UserObject( username, password));
        }

        public void Delete()
        {
            using (var c = new ToyDbContext(ConnectionString.value))
            {
                var siteToRemove = c.Sites.SingleOrDefault(s => s.Name == Name && s.Timezone == Timezone);
                if (siteToRemove == default)
                    throw new AuctionSiteArgumentNullException("The site doesn't exists.");

                siteToRemove.ActiveUsers.RemoveAll(u => true);
                siteToRemove.ActiveSessions.RemoveAll(u => true);

                c.Sites.Remove(siteToRemove);
                c.SaveChanges();
            }

            //ActiveUsers.RemoveAll(u => true);
            //ActiveSessions.RemoveAll(u => true);
        }

        public DateTime Now()
        {
            return AlarmClock.Now;
        }
    }

    public class SessionObject : ISession
    {
        public string Id { get; set; }
        public DateTime ValidUntil { get; }
        public IUser User { get; }
        public double MinimumBidIncrement { get; }

        public List<AuctionObject> ActiveAuctions { get; }
        //public event EventHandler currentTimeLeft;

        public SessionObject(DateTime validUntil, double minimumBidIncrement, IUser user)
        {
            ValidUntil = validUntil;
            MinimumBidIncrement = minimumBidIncrement;
            User = user;
            ActiveAuctions = new List<AuctionObject>();
        }

        public void Logout()
        {
            //TODO: campo "logged" nella ToyDbContext.Session?

            throw new NotImplementedException();
        }

        public IAuction CreateAuction(string description, DateTime endsOn, double startingPrice)
        {
            if (description == null) throw new AuctionSiteArgumentNullException("Description cannot be null");
            if (description == "") throw new AuctionSiteArgumentException("Description cannot be empty");
            if (startingPrice < 0)
                throw new AuctionSiteArgumentOutOfRangeException("Starting price cannot be negative");


            using (var c = new ToyDbContext(new string("ciao")))
            {
                var dbSession = c.Sessions.SingleOrDefault(s => s.Id == Id);
                if (dbSession == default)
                    throw new AuctionSiteArgumentNullException("Auction not found.");

                if (c.Auctions.Any(a =>
                        a.Description == description && a.EndsOn == endsOn && startingPrice == startingPrice))
                    throw new AuctionSiteArgumentNullException("The auction already exists!");

                var newDbAuction = new ToyDbContext.DbAuction(dbSession.User, description, endsOn, MinimumBidIncrement,
                    startingPrice);

                c.Auctions.Add(newDbAuction);
                dbSession.ActiveAuctions.Add(newDbAuction);
                c.SaveChanges();
            }

            var newAuction = new AuctionObject(User, description, endsOn, MinimumBidIncrement, startingPrice);
            ActiveAuctions.Add(newAuction);
            return newAuction;
        }

    }

    public class UserObject : IUser
    {
        [Key] public string Username { get; set; }
        public string Password { get; }

        public UserObject()
        {
        }

        public UserObject(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public IEnumerable<IAuction> WonAuctions()
        {
            throw new NotImplementedException();
            using (var c = new ToyDbContext(new string("ciao")))
            {
                //var auctions = c.Auctions.Where(a => a.Ended && a.activeBids.Max().Key==Username);

                //foreach (var a in auctions)
                //  yield return new AuctionObject(new UserObject(a.Seller.Username,a.Seller.Password),a.Description,a.EndsOn,a.MinimumBidIncrement,a.StartingPrice);

            }
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }
    }

    public class AuctionObject : IAuction
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public IUser Seller { get; }
        public string Description { get; }
        public DateTime EndsOn { get; }
        public double MinimumBidIncrement { get; }
        public double StartingPrice { get; }
        public bool Ended { get; set; }
        public Dictionary<IUser, double> ActiveBids { get; }

        public AuctionObject()
        {
        }

        public AuctionObject(IUser seller, string description, DateTime endsOn, double minumumBidIncrement,
            double startingPrice)
        {
            Seller = seller;
            Description = description;
            EndsOn = endsOn;
            MinimumBidIncrement = minumumBidIncrement;
            StartingPrice = startingPrice;
            Ended = false;
            ActiveBids = new Dictionary<IUser, double>();
        }

        public IUser? CurrentWinner()
        {
            return !ActiveBids.Any() ? null : ActiveBids.Max().Key;
        }

        public double CurrentPrice()
        {
            return !ActiveBids.Any() ? StartingPrice : ActiveBids.Max().Value;
        }

        public void Delete()
        {
            using (var c = new ToyDbContext(new string("ciao")))
            {
                var auction = c.Auctions.SingleOrDefault(a => a.Id == Id);
                if (default == auction)
                    throw new AuctionSiteArgumentNullException("Auction not found");
                c.Auctions.Remove(auction);
                c.SaveChanges();

                Ended = true;
            }
        }

        public bool Bid(ISession session, double offer)
        {
            //Check if the session is valid

            var user = session.User;
            var topClient = ActiveBids.Max().Key;
            var topValue = ActiveBids.Max().Value;

            if ((offer - topValue) > MinimumBidIncrement)
            {
                ActiveBids.Add(user, offer);
                return true;
            }

            return false;
        }
    }
}