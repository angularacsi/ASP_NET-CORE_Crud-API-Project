namespace Crud.Exceptions
{
    public class InactiveAccountException : Exception
    {
        public InactiveAccountException()
            : base("User account is inactive.") { }
    }
}
