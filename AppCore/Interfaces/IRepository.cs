using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppCore.Interfaces
{
    public interface IRepository<T> where T : IEntity
    {
        T? GetById(string id);        
        T Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        bool RecordExists(string id);        
    }
}