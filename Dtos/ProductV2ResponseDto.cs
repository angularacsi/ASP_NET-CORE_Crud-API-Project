namespace Crud.Dtos
{
    public class ProductV2ResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "ETB";
    }
}
