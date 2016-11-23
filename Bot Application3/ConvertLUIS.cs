using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bot_Application3
{
    public class ConvertLUIS
    {
        public string query { get; set; }
        public Topscoringintent topScoringIntent { get; set; }
        public Intents[] intents { get; set; }
        public Entity[] entities { get; set; }
    }

    public class Topscoringintent
    {
        public string intent { get; set; }
        public float score { get; set; }
    }

    public class Intents
    {
        public string intent { get; set; }
        public float score { get; set; }
    }

    public class Entity
    {
        public string entity { get; set; }
        public string type { get; set; }
        public int startIndex { get; set; }
        public int endIndex { get; set; }
        public float score { get; set; }
    }
}