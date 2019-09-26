using System.Linq;

namespace Repository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> GetAll();
    }

}
