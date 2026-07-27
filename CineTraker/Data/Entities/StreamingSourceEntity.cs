using System.ComponentModel.DataAnnotations;

namespace CineTraker.Data.Entities
{
    public class StreamingSourceEntity
    {
        [Key] 
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string WebUrl { get; set; } = "";
        public string LogoUrl { get; set; } = "";
        public List<MovieEntity> Movies { get; set; } = new();
    }
}
