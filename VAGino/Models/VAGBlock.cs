using SQLite;

namespace VAGino.Models
{
    public class VAGBlock
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
