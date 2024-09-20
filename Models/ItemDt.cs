namespace TestPOSApp.Models
{
    public class ItemDt
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; }
        public IFormFile ImageFile { get; set; }
        public string ImageUrl { get; set; }
    }
}
