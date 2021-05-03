using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SignerApi.Data;
using SignerApi.Models;

namespace SignerApi.Services
{
    public class ApiActivityService : IApiActivityService
    {
        private readonly DataContext _db;
        private readonly ILogger _l;
        public ApiActivityService(DataContext db, ILogger logger)
        {
            _db = db;
            _l = logger;
        }
        

        /// <summary>
        /// Adds ApiActivity Object to DB or updates existing object.
        /// </summary>
        /// <param name="ac">ApiActivity to add or update</param>
        public void addUpdateApiActivity(ApiActivity ac)
        {
            var entity = _db.ApiActivity.FirstOrDefault(item => item.Key == ac.Key);

            // entity already exists -> update
            if (entity != null)
            {
                _db.Entry(entity).CurrentValues.SetValues(ac);
                _db.ApiActivity.Update(entity);
                _db.SaveChanges();
                _l.Debug($"Updated in DB: {ac.ToString()}");
            }
            else
            {
                // create new
                _db.ApiActivity.Add(ac);
                _db.SaveChanges();
                _l.Debug($"Added to DB: {ac.ToString()}");
            }
        }

        public ApiActivity getActivityByUniqueKey(string key)
        {
            ApiActivity ac = null;
            ac = _db.ApiActivity.FirstOrDefault(item => item.UniqueKey == key);
            return ac;
        }

        
        /// <summary>
        /// Select all items in DB with queued for analyse status.
        /// </summary>
        /// <returns>List with ApiActivity with Status = QueuedAnalysis</returns>
        public List<ApiActivity> getItemsToBeAnalysed()
        {
            List<ApiActivity> l = new List<ApiActivity>();
            l = _db.ApiActivity
                .Where(a => a.Status == ApiActivity.ApiStatus.QueuedAnalysis)
                .ToList();

            return l;
        }


        /// <summary>
        /// Select all items in DB with queued for signing status.
        /// </summary>
        /// <returns>List with ApiActivity with Status = QueuedSigning</returns>
        public List<ApiActivity> getItemsToBeSigned()
        {
            List<ApiActivity> l = new List<ApiActivity>();
            l = _db.ApiActivity
                .Where(a => a.Status == ApiActivity.ApiStatus.QueuedSigning)
                .ToList();

            return l;
        }
    }
}
