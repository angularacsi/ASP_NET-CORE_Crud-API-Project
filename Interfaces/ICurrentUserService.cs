namespace Crud.Interfaces
{
    public interface ICurrentUserService
    {
        string GetCurrentUsername();
        string GetCurrentUserRole();
        public int  GetCurrentUserId();
        string GetEmail();


    }
}
