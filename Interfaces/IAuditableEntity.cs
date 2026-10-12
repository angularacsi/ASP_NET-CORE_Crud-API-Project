namespace Crud.Interfaces
{
    public interface IAuditableEntity
    {
        int CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }

        int? UpdatedBy { get; set; }
        DateTime? UpdatedDate { get; set; }
    }
}
