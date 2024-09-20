namespace TestPOSApp.Models
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalPrice { get; set; }
        public List<TransactionItemDto> Items { get; set; }
    }
}
