using System.Collections.Generic;
using SignerApi.Models;

namespace SignerApi.Services
{
    public interface IApiActivityService
    {
        /// <summary>
        /// checks if entity exists. if yes, update. If not, create new one.
        /// </summary>
        /// <param name="ac">Activity to update / add</param>
        public void addUpdateApiActivity(ApiActivity ac);

        /// <summary>
        /// Returns existing API Activity by Key
        /// </summary>
        /// <param name="key">GUID key</param>
        /// <returns>ApiActivity if found in DB or null</returns>
        public ApiActivity getActivityByUniqueKey(string key);



        public List<ApiActivity> getItemsToBeAnalysed();
    }
}