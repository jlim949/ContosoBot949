using Bot_Application3.Models;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Bot_Application3
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<ContosoTable949> timelineTable;
        
        private AzureManager()
        {
            this.client = new MobileServiceClient("http://contosomobile949.azurewebsites.net");

            this.timelineTable = this.client.GetTable<ContosoTable949>();
        
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }
        public async Task<List<ContosoTable949>> GetTimelines()
        {
            return await this.timelineTable.ToListAsync();
        }
        public async Task AddTimeline(ContosoTable949 timeline)
        {
            await this.timelineTable.InsertAsync(timeline);
        }
        public async Task UpdateTimeline(ContosoTable949 timeline)
        {
            await this.timelineTable.UpdateAsync(timeline);
        }
        public async Task DeleteTimeline(ContosoTable949 timeline)
        {
            await this.timelineTable.DeleteAsync(timeline);
        }
    }
}