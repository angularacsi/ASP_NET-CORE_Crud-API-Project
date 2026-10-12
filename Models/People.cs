namespace Crud.Models
{
    public class People
    {
        public int Id { get; set; }
        required
        public string? Name { get; set; }
        required
        public string? email { get; set; }
        required
        public string? address { get; set; }
    }
}
