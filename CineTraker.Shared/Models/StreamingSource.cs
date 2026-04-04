using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineTraker.Shared
{
        public class StreamingSource
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = ""; // sub, rent, buy
            public string WebUrl { get; set; } = "";
            public string LogoUrl { get; set; } = "";
 
        }


    }
