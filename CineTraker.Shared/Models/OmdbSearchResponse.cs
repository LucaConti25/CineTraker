using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared.Models
{
    public class OmdbSearchResponse
    {
        public List<OmdbResponse>? Search { get; set; }
        public string? totalResults { get; set; }
        public string? Response { get; set; }
    }
}
