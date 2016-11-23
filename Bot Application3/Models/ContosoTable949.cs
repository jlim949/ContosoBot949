using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;

namespace Bot_Application3.Models
{
    public class ContosoTable949
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Balance")]
        public double Balance { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime Date { get; set; }
    }
}