using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CineTraker.Shared
{
        public class StreamingSource
        {
            [Key] 
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Type { get; set; } = ""; // sub, rent, buy
            public string WebUrl { get; set; } = "";
            public string LogoUrl { get; set; } = "";
            [JsonIgnore]
            public List<Movie> Movies { get; set; } = new();

    }


    }
