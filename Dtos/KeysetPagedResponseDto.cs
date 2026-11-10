namespace Crud.Dtos
{
    public class KeysetPagedResponseDto<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();

        public bool HasMore { get; set; }

        public int? NextCursor { get; set; }
    }
}
